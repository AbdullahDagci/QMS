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
            if (string.IsNullOrEmpty(auditEvent.IntegrityHash))
            {
                var hash = RecordIntegrityService.AuditHash(auditEvent, previousHash);
                var mac = integrity.Mac(hash);
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE audit.audit_event
                    SET "PreviousIntegrityHash" = {previousHash}, "IntegrityHash" = {hash}, "IntegrityMac" = {mac}
                    WHERE "Id" = {auditEvent.Id} AND "IntegrityHash" = ''
                    """, cancellationToken);
                previous[key] = hash;
                continue;
            }
            if (auditEvent.PreviousIntegrityHash != previousHash
                || !RecordIntegrityService.IsAuditHashValid(auditEvent, previousHash)
                || !integrity.VerifyMac(auditEvent.IntegrityHash, auditEvent.IntegrityMac))
                throw new InvalidOperationException(
                    $"Denetim izi bütünlük ihlali algılandı. AuditEventId={auditEvent.Id}");
            previous[key] = auditEvent.IntegrityHash;
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
