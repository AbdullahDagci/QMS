namespace Qms.Contracts.Deviations;

public sealed record DeviationListItemResponse(
    Guid Id,
    string RecordNumber,
    string Title,
    string DetectedDepartment,
    int RiskScore,
    string Classification,
    bool CapaRequired,
    string Status,
    DateTimeOffset TargetDateUtc,
    DateTimeOffset CreatedAtUtc,
    long Version);
