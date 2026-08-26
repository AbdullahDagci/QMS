namespace Qms.Domain.Deviations;

public sealed class DeviationInvestigation
{
    private DeviationInvestigation()
    {
    }

    public Guid Id { get; private set; }

    public Guid DeviationId { get; private set; }

    public string Method { get; private set; } = string.Empty;

    public string RootCauseCategory { get; private set; } = string.Empty;

    public string RootCauseDescription { get; private set; } = string.Empty;

    public string Conclusion { get; private set; } = string.Empty;

    public Guid InvestigatorUserId { get; private set; }

    public string InvestigatorNameSnapshot { get; private set; } = string.Empty;

    public string InvestigatorDepartmentSnapshot { get; private set; } = string.Empty;

    public DateTimeOffset CompletedAtUtc { get; private set; }

    public static DeviationInvestigation CreateCompleted(
        Guid deviationId,
        string method,
        string rootCauseCategory,
        string rootCauseDescription,
        string conclusion,
        Guid investigatorUserId,
        string investigatorName,
        string investigatorDepartment,
        DateTimeOffset completedAtUtc)
    {
        if (deviationId == Guid.Empty)
        {
            throw new ArgumentException("Sapma kimliği zorunludur.", nameof(deviationId));
        }

        ValidateText(method, nameof(method), 120);
        ValidateText(rootCauseCategory, nameof(rootCauseCategory), 120);
        ValidateText(rootCauseDescription, nameof(rootCauseDescription), 4000);
        ValidateText(conclusion, nameof(conclusion), 4000);
        ValidateText(investigatorName, nameof(investigatorName), 200);
        ValidateText(investigatorDepartment, nameof(investigatorDepartment), 160);

        return new DeviationInvestigation
        {
            Id = Guid.CreateVersion7(),
            DeviationId = deviationId,
            Method = method.Trim(),
            RootCauseCategory = rootCauseCategory.Trim(),
            RootCauseDescription = rootCauseDescription.Trim(),
            Conclusion = conclusion.Trim(),
            InvestigatorUserId = investigatorUserId,
            InvestigatorNameSnapshot = investigatorName.Trim(),
            InvestigatorDepartmentSnapshot = investigatorDepartment.Trim(),
            CompletedAtUtc = completedAtUtc
        };
    }

    private static void ValidateText(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Trim().Length > maximumLength)
        {
            throw new ArgumentException($"Alan en fazla {maximumLength} karakter olabilir.", parameterName);
        }
    }
}
