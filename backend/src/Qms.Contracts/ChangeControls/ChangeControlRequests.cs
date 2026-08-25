using Qms.Contracts.Common;

namespace Qms.Contracts.ChangeControls;

public sealed record CreateChangeControlRequest(
    Guid? SourceCapaId,
    string ChangeType,
    string Title,
    string CurrentState,
    string ProposedState,
    string Justification,
    string Scope,
    bool IsTemporary,
    DateTimeOffset? TemporaryUntilUtc,
    string Owner,
    DateTimeOffset TargetDateUtc,
    string RiskLevel,
    string RiskSummary,
    bool ProductImpact,
    bool SiteImpact,
    bool ValidationRequired,
    string RegulatoryImpact,
    string RollbackPlan,
    IReadOnlyList<string>? ImpactedDepartments);

public sealed record ChangeControlSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record CompleteChangeAssessmentRequest(long ExpectedVersion, bool Approved, string ImpactSummary, string RequiredActions = "Yok");
public sealed record AddChangeActionRequest(long ExpectedVersion, string Category, string Description, string Owner, DateTimeOffset TargetDateUtc, bool IsBlocking = true);
public sealed record CompleteChangeActionRequest(long ExpectedVersion, string Evidence);
public sealed record VerifyChangeActionRequest(long ExpectedVersion, bool Approved, string Note);
public sealed record SetAuthorityApprovalRequest(long ExpectedVersion, string Reference);
public sealed record TransitionChangeControlRequest(long ExpectedVersion, string Transition, string? Note = null, bool Successful = true);
