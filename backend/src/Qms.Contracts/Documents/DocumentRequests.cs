using Qms.Contracts.Common;
namespace Qms.Contracts.Documents;

public sealed record CreateControlledDocumentRequest(Guid? SourceChangeControlId, string DocumentCode, string Title, string DocumentType, string Owner, string Department, string Confidentiality, int ReviewPeriodMonths, DateTimeOffset PlannedEffectiveDateUtc, string Content, string ChangeSummary, IReadOnlyList<string>? ReviewDepartments, IReadOnlyList<string>? TrainingPositions);
public sealed record ControlledDocumentSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record UpdateDocumentDraftRequest(long ExpectedVersion, string Content, string ChangeSummary);
public sealed record CompleteDocumentReviewRequest(long ExpectedVersion, bool Approved, string Comment);
public sealed record CompleteDocumentTrainingRequest(long ExpectedVersion, string Evidence);
public sealed record StartDocumentRevisionRequest(long ExpectedVersion, bool Major, string ChangeSummary, IReadOnlyList<string>? ReviewDepartments, IReadOnlyList<string>? TrainingPositions);
public sealed record IssueControlledCopyRequest(long ExpectedVersion, string CopyNumber, string Recipient, string Purpose, DateTimeOffset? DueBackAtUtc);
public sealed record CloseControlledCopyRequest(long ExpectedVersion, bool Destroyed);
public sealed record AcknowledgeDocumentReadRequest(long ExpectedVersion, string SignatureMeaning);
public sealed record TransitionDocumentRequest(long ExpectedVersion, string Transition, string? Note = null, bool RequiresRevision = false);
