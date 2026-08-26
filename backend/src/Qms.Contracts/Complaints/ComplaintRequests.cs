using Qms.Contracts.Common;

namespace Qms.Contracts.Complaints;

public sealed record ComplaintSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record CreateComplaintRequest(string Channel, string CustomerName, string Country, string Product, string? BatchNumber, DateTimeOffset EventAtUtc, DateTimeOffset ReceivedAtUtc, string ComplaintType, string Description, string Severity, bool HasHealthImpact, bool SuspectedAdverseEvent, bool SampleExpected, bool ReturnExpected, string? AttachmentSummary, Guid OwnerUserId, DateTimeOffset PreliminaryResponseDueAtUtc, DateTimeOffset FinalResponseDueAtUtc, IReadOnlyList<Guid> InvestigationDepartmentIds);
public sealed record TransitionComplaintRequest(long ExpectedVersion, string Transition, string? Note = null, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
public sealed record AddComplaintResponseRequest(long ExpectedVersion, string ResponseType, string Content);
public sealed record ApproveComplaintResponseRequest(long ExpectedVersion, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
public sealed record CompleteComplaintInvestigationRequest(long ExpectedVersion, string Findings, string RootCauseContribution);
public sealed record CompleteComplaintImpactRequest(long ExpectedVersion, string ImpactAssessment, string ConfirmedRootCause);
public sealed record DecideComplaintCapaRequest(long ExpectedVersion, bool CapaRequired, Guid? OwnerUserId, DateTimeOffset? TargetDateUtc, bool EffectivenessRequired = true);
public sealed record CreateComplaintLookupDefinitionRequest(string Category, string Code, string Name, int SortOrder);
public sealed record UpdateComplaintLookupDefinitionRequest(string Name, int SortOrder, bool IsActive);

