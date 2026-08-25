namespace Qms.Contracts.Deviations;

public sealed record CreateDeviationRequest(
    string Title,
    string Description,
    string ExpectedState,
    string ImmediateAction,
    string DeviationType,
    string DetectedDepartment,
    string ProcessStage,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset DetectedAtUtc,
    int Likelihood,
    int Severity,
    int Detectability);
