namespace Qms.Contracts.Deviations;

public sealed record AddDeviationBatchImpactRequest(
    long ExpectedVersion,
    string BatchNumber,
    bool IsAffected,
    bool IsLocked,
    string Disposition,
    string Rationale);
