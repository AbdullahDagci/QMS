namespace Qms.Domain.ChangeControls;

public enum ChangeAssessmentStatus { Pending, Approved, Rejected }

public sealed class ChangeAssessment
{
    private ChangeAssessment() { }

    public Guid Id { get; private set; }
    public Guid ChangeControlId { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public string Reviewer { get; private set; } = string.Empty;
    public Guid? ReviewerUserId { get; private set; }
    public ChangeAssessmentStatus Status { get; private set; }
    public string? ImpactSummary { get; private set; }
    public string? RequiredActions { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    internal static ChangeAssessment Create(Guid changeControlId, Guid departmentId, string department, Guid reviewerUserId, string reviewer)
    {
        Text(department, nameof(department), 120); Text(reviewer, nameof(reviewer), 160);
        if (departmentId == Guid.Empty || reviewerUserId == Guid.Empty) throw new ArgumentException("Bölüm ve değerlendirici seçimi zorunludur.");
        return new ChangeAssessment { Id = Guid.CreateVersion7(), ChangeControlId = changeControlId, DepartmentId = departmentId, Department = department.Trim(), ReviewerUserId = reviewerUserId, Reviewer = reviewer.Trim(), Status = ChangeAssessmentStatus.Pending };
    }

    internal void Complete(bool approved, string impactSummary, string requiredActions, DateTimeOffset now)
    {
        Text(impactSummary, nameof(impactSummary), 2000);
        if (Status != ChangeAssessmentStatus.Pending) throw new InvalidOperationException("Bölüm değerlendirmesi daha önce tamamlanmış.");
        Status = approved ? ChangeAssessmentStatus.Approved : ChangeAssessmentStatus.Rejected;
        ImpactSummary = impactSummary.Trim(); RequiredActions = requiredActions.Trim(); CompletedAtUtc = now;
    }

    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
