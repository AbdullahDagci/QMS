using Qms.Contracts.Common;

namespace Qms.Contracts.Complaints;

public sealed record ComplaintSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record CreateComplaintRequest(string Channel, string CustomerName, string Country, string Product, string? BatchNumber, DateTimeOffset EventAtUtc, DateTimeOffset ReceivedAtUtc, string ComplaintType, string Description, string Severity, bool HasHealthImpact, bool SuspectedAdverseEvent, bool SampleExpected, bool ReturnExpected, string? AttachmentSummary, string Owner, DateTimeOffset PreliminaryResponseDueAtUtc, DateTimeOffset FinalResponseDueAtUtc, IReadOnlyList<string> InvestigationDepartments);
public sealed record TransitionComplaintRequest(long ExpectedVersion, string Transition, string? Note = null);
public sealed record AddComplaintResponseRequest(long ExpectedVersion, string ResponseType, string Content);
public sealed record ApproveComplaintResponseRequest(long ExpectedVersion);
public sealed record CompleteComplaintInvestigationRequest(long ExpectedVersion, string Findings, string RootCauseContribution);
public sealed record CompleteComplaintImpactRequest(long ExpectedVersion, string ImpactAssessment, string ConfirmedRootCause);
public sealed record DecideComplaintCapaRequest(long ExpectedVersion, bool CapaRequired, string? Owner, DateTimeOffset? TargetDateUtc, bool EffectivenessRequired = true);

