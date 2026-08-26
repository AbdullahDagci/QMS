namespace Qms.Domain.Deviations;

public sealed class DeviationAssignmentRule
{
    private DeviationAssignmentRule() { }
    public Guid Id { get; private set; }
    public string TaskRole { get; private set; } = string.Empty;
    public Guid AssignedUserId { get; private set; }
    public string? DetectedDepartment { get; private set; }
    public string? DeviationType { get; private set; }
    public int? MinimumRiskScore { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    public static DeviationAssignmentRule Create(string taskRole, Guid assignedUserId, string? detectedDepartment, string? deviationType, int? minimumRiskScore, int priority)
    {
        var item = new DeviationAssignmentRule { Id = Guid.CreateVersion7() };
        item.Update(taskRole, assignedUserId, detectedDepartment, deviationType, minimumRiskScore, priority, true);
        return item;
    }

    public void Update(string taskRole, Guid assignedUserId, string? detectedDepartment, string? deviationType, int? minimumRiskScore, int priority, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskRole);
        if (assignedUserId == Guid.Empty) throw new ArgumentException("Atanacak kullanıcı zorunludur.", nameof(assignedUserId));
        if (minimumRiskScore is < 1 or > 125) throw new ArgumentOutOfRangeException(nameof(minimumRiskScore), "Minimum RPN 1-125 arasında olmalıdır.");
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority));
        TaskRole = taskRole.Trim(); AssignedUserId = assignedUserId;
        DetectedDepartment = string.IsNullOrWhiteSpace(detectedDepartment) ? null : detectedDepartment.Trim();
        DeviationType = string.IsNullOrWhiteSpace(deviationType) ? null : deviationType.Trim();
        MinimumRiskScore = minimumRiskScore; Priority = priority; IsActive = isActive;
    }
}
