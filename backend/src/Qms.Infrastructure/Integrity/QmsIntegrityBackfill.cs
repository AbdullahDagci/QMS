using Microsoft.EntityFrameworkCore;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Integrity;

public sealed class QmsIntegrityBackfill(QmsDbContext db, RecordIntegrityService integrity)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!integrity.IsConfigured)
            throw new InvalidOperationException("Kayıt bütünlüğü backfill işlemi için HMAC anahtarı zorunludur.");

        var auditEvents = await db.AuditEvents.AsNoTracking()
            .OrderBy(item => item.AggregateType).ThenBy(item => item.AggregateId)
            .ThenBy(item => item.OccurredAtUtc).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var previous = new Dictionary<(string Type, Guid Id), string>();
        foreach (var auditEvent in auditEvents)
        {
            var key = (Type: auditEvent.AggregateType, Id: auditEvent.AggregateId);
            previous.TryGetValue(key, out var previousHash);
            previousHash ??= string.Empty;
            var hash = RecordIntegrityService.AuditHash(auditEvent, previousHash);
            var mac = integrity.Mac(hash);
            if (string.IsNullOrEmpty(auditEvent.IntegrityHash))
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE audit.audit_event
                    SET "PreviousIntegrityHash" = {previousHash}, "IntegrityHash" = {hash}, "IntegrityMac" = {mac}
                    WHERE "Id" = {auditEvent.Id} AND "IntegrityHash" = ''
                    """, cancellationToken);
            else if (auditEvent.PreviousIntegrityHash != previousHash
                     || !string.Equals(auditEvent.IntegrityHash, hash, StringComparison.OrdinalIgnoreCase)
                     || !integrity.VerifyMac(hash, auditEvent.IntegrityMac))
                throw new InvalidOperationException(
                    $"Denetim izi bütünlük ihlali algılandı. AuditEventId={auditEvent.Id}");
            previous[key] = hash;
        }

        var signatures = await db.ElectronicSignatures.AsNoTracking()
            .Where(item => item.ProviderType == "Internal")
            .ToListAsync(cancellationToken);
        foreach (var signature in signatures)
        {
            var payload = RecordIntegrityService.SignaturePayload(signature);
            if (string.IsNullOrEmpty(signature.IntegrityMac))
            {
                var mac = integrity.Mac(payload);
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE core.electronic_signature SET "IntegrityMac" = {mac}
                    WHERE "Id" = {signature.Id} AND "IntegrityMac" = ''
                    """, cancellationToken);
            }
            else if (!integrity.VerifyMac(payload, signature.IntegrityMac))
                throw new InvalidOperationException(
                    $"Elektronik imza bütünlük ihlali algılandı. SignatureId={signature.Id}");
        }
    }
}
