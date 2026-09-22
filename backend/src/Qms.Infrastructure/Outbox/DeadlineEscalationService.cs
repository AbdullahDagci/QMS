using Microsoft.EntityFrameworkCore;
using Qms.Domain.Notifications;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Outbox;

public sealed class DeadlineEscalationService(QmsDbContext db, TimeProvider timeProvider)
{
    public async Task<int> EscalateAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var threshold = now.AddHours(-24);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var tasks = await db.WorkflowTaskAssignments.FromSqlInterpolated($"""
            SELECT * FROM workflow.task_assignment
            WHERE "Status" = 'Active' AND "DueAtUtc" <= {now}
              AND ("LastEscalatedAtUtc" IS NULL OR "LastEscalatedAtUtc" <= {threshold})
            ORDER BY "DueAtUtc"
            LIMIT 100 FOR UPDATE SKIP LOCKED
            """).ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            var level = task.Escalate(now);
            var module = ModuleCode(task.AggregateType);
            var link = ModuleLink(task.AggregateType, task.AggregateId);
            db.UserNotifications.Add(UserNotification.Create(task.AssignedUserId, module,
                $"Gecikmiş görev · seviye {level}",
                $"{task.TaskRole} görevinin hedef tarihi geçti. Lütfen kaydı inceleyin.", link, now));
            if (level >= 2 && task.AssignedDepartmentId.HasValue)
            {
                var managerId = await db.Departments.AsNoTracking()
                    .Where(department => department.Id == task.AssignedDepartmentId.Value)
                    .Select(department => department.ManagerUserId).SingleOrDefaultAsync(cancellationToken);
                if (managerId.HasValue && managerId != task.AssignedUserId)
                    db.UserNotifications.Add(UserNotification.Create(managerId.Value, module,
                        $"Bölüm eskalasyonu · {task.TaskRole}",
                        $"Bölümünüzdeki bir görev {level}. eskalasyon seviyesine ulaştı.", link, now));
            }
        }
        if (tasks.Count > 0) await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return tasks.Count;
    }

    private static string ModuleCode(string type) => type switch
    {
        "Deviation" => "M.01", "Capa" => "M.02", "ChangeControl" => "M.03",
        "Document" => "M.04", "Training" => "M.05", "Complaint" => "M.06",
        "InternalAudit" => "M.07", "ExternalAudit" => "M.08", "SupplierAudit" => "M.09",
        "WorkItem" => "M.10", "RiskAssessment" => "M.11", "MasterBatchRecord" => "M.12",
        _ => "QMS"
    };

    private static string ModuleLink(string type, Guid id) => type switch
    {
        "Deviation" => $"/modules/deviations?open={id}",
        "Capa" => $"/modules/m02?open={id}", "ChangeControl" => $"/modules/m03?open={id}",
        "Document" => $"/modules/m04?open={id}", "Training" => $"/modules/m05?open={id}",
        "Complaint" => $"/modules/m06?open={id}", "InternalAudit" => $"/modules/m07?open={id}",
        "ExternalAudit" => $"/modules/m08?open={id}", "SupplierAudit" => $"/modules/m09?open={id}",
        "WorkItem" => $"/modules/m10?open={id}", "RiskAssessment" => $"/modules/m11?open={id}",
        "MasterBatchRecord" => $"/modules/m12?open={id}", _ => "/"
    };
}
