using Microsoft.EntityFrameworkCore;
using Qms.Domain.Organization;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Security;

public sealed class RecordAccessBackfill(QmsDbContext db)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var existingAssignmentIds = await db.RecordAccessGrants.AsNoTracking()
            .Select(grant => grant.AssignmentId).ToHashSetAsync(cancellationToken);
        var assignments = await db.WorkflowTaskAssignments.AsNoTracking()
            .Where(assignment => !existingAssignmentIds.Contains(assignment.Id))
            .OrderBy(assignment => assignment.AssignedAtUtc).ToListAsync(cancellationToken);
        foreach (var assignment in assignments)
        {
            var qualityRecordId = await db.ResolveQualityRecordIdAsync(assignment.AggregateType,
                assignment.AggregateId, cancellationToken);
            if (qualityRecordId.HasValue)
                db.RecordAccessGrants.Add(RecordAccessGrant.Create(qualityRecordId.Value,
                    assignment.AssignedUserId, assignment.Id, assignment.AssignedAtUtc));
        }
        if (assignments.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }
}
