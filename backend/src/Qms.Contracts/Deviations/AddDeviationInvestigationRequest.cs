namespace Qms.Contracts.Deviations;

public sealed record AddDeviationInvestigationRequest(
    long ExpectedVersion,
    string Method,
    string RootCauseCategory,
    string RootCauseDescription,
    string Conclusion);
