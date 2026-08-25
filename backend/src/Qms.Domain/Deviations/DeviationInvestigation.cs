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

    public DateTimeOffset CompletedAtUtc { get; private set; }

    public static DeviationInvestigation CreateCompleted(
        Guid deviationId,
        string method,
        string rootCauseCategory,
        string rootCauseDescription,
        string conclusion,
        Guid investigatorUserId,
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

        return new DeviationInvestigation
        {
            Id = Guid.CreateVersion7(),
            DeviationId = deviationId,
            Method = method.Trim(),
            RootCauseCategory = rootCauseCategory.Trim(),
            RootCauseDescription = rootCauseDescription.Trim(),
            Conclusion = conclusion.Trim(),
            InvestigatorUserId = investigatorUserId,
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
