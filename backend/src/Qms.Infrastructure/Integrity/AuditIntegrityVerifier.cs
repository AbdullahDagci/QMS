using Microsoft.EntityFrameworkCore;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Integrity;

public sealed class AuditIntegrityVerifier(QmsDbContext db, RecordIntegrityService integrity)
{
    public async Task<AuditIntegrityResult> VerifyAsync(string aggregateType, Guid aggregateId,
        CancellationToken cancellationToken)
    {
        var events = await db.AuditEvents.AsNoTracking()
            .Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId)
            .OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var previous = string.Empty;
        foreach (var auditEvent in events)
        {
            if (auditEvent.PreviousIntegrityHash != previous
                || !RecordIntegrityService.IsAuditHashValid(auditEvent, previous)
                || !integrity.VerifyMac(auditEvent.IntegrityHash, auditEvent.IntegrityMac))
                return new AuditIntegrityResult(false, events.Count, auditEvent.Id,
                    "Denetim izi zinciri veya anahtarlı bütünlük mührü doğrulanamadı.");
            previous = auditEvent.IntegrityHash;
        }
        return new AuditIntegrityResult(true, events.Count, null,
            events.Count == 0 ? "Doğrulanacak denetim izi bulunamadı." : "Denetim izi zinciri doğrulandı.");
    }
}

public sealed record AuditIntegrityResult(bool IsValid, int EventCount, Guid? FailedEventId, string Message);
