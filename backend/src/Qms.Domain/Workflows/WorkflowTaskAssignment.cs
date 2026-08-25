namespace Qms.Domain.Workflows;

public enum WorkflowTaskStatus { Active, Completed, Cancelled }

public sealed class WorkflowTaskAssignment
{
    private WorkflowTaskAssignment() { }

    public Guid Id { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string TaskRole { get; private set; } = string.Empty;
    public Guid AssignedUserId { get; private set; }
    public Guid? AssignedDepartmentId { get; private set; }
    public Guid? DelegatedFromUserId { get; private set; }
    public WorkflowTaskStatus Status { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public DateTimeOffset? DueAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static WorkflowTaskAssignment Create(string aggregateType, Guid aggregateId, string taskRole, Guid assignedUserId, Guid? assignedDepartmentId, DateTimeOffset assignedAtUtc, DateTimeOffset? dueAtUtc = null, Guid? delegatedFromUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType); ArgumentException.ThrowIfNullOrWhiteSpace(taskRole);
        return new WorkflowTaskAssignment { Id = Guid.CreateVersion7(), AggregateType = aggregateType.Trim(), AggregateId = aggregateId, TaskRole = taskRole.Trim(), AssignedUserId = assignedUserId, AssignedDepartmentId = assignedDepartmentId, DelegatedFromUserId = delegatedFromUserId, Status = WorkflowTaskStatus.Active, AssignedAtUtc = assignedAtUtc, DueAtUtc = dueAtUtc };
    }

    public void Complete(DateTimeOffset now) { Status = WorkflowTaskStatus.Completed; CompletedAtUtc = now; }
    public void Cancel() => Status = WorkflowTaskStatus.Cancelled;
}
