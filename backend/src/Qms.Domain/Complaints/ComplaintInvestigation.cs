namespace Qms.Domain.Complaints;

public sealed class ComplaintInvestigation
{
    private ComplaintInvestigation() { }
    public Guid Id { get; private set; }
    public Guid ComplaintId { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string Investigator { get; private set; } = string.Empty;
    public ComplaintInvestigationStatus Status { get; private set; }
    public string? Findings { get; private set; }
    public string? RootCauseContribution { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    internal static ComplaintInvestigation Create(Guid complaintId, string department, string investigator)
    {
        Text(department, nameof(department), 160); Text(investigator, nameof(investigator), 160);
        return new() { Id = Guid.CreateVersion7(), ComplaintId = complaintId, Department = department.Trim(), Investigator = investigator.Trim(), Status = ComplaintInvestigationStatus.Pending };
    }

    internal void Complete(string findings, string rootCauseContribution, DateTimeOffset now)
    {
        if (Status == ComplaintInvestigationStatus.Completed) throw new InvalidOperationException("Araştırma zaten tamamlanmış.");
        Text(findings, nameof(findings), 4000); Text(rootCauseContribution, nameof(rootCauseContribution), 2000);
        Findings = findings.Trim(); RootCauseContribution = rootCauseContribution.Trim(); Status = ComplaintInvestigationStatus.Completed; CompletedAtUtc = now;
    }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}

