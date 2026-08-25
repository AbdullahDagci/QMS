namespace Qms.Contracts.Dashboard;

public sealed record DashboardSummaryResponse(
    long TotalDeviations,
    long OpenDeviations,
    long MajorOrCriticalDeviations,
    long OverdueDeviations,
    long CapaRequiredDeviations,
    IReadOnlyList<DashboardDeviationResponse> RecentDeviations);

public sealed record DashboardDeviationResponse(
    Guid Id,
    string RecordNumber,
    string Title,
    string Status,
    string Classification,
    int RiskScore,
    DateTimeOffset TargetDateUtc);
