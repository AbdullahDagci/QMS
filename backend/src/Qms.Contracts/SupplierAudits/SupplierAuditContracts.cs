using System.Text.Json;
using Qms.Contracts.Common;

namespace Qms.Contracts.SupplierAudits;

public sealed record SupplierAuditSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record SupplierAuditChecklistInput(string Category, string Question, string Reference);
public sealed record CreateSupplierAuditRequest(Guid? SupplierEvaluationId, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Country, string Criticality, decimal PastPerformanceScore, int OpenFindingCount, string Scope, string Site, Guid LeadAuditorUserId, string LeadAuditor, string LeadAuditorDepartment, string PurchasingOwner, DateTimeOffset PlannedStartUtc, DateTimeOffset PlannedEndUtc, string ChecklistVersion, IReadOnlyList<SupplierAuditChecklistInput> Checklist);
public sealed record TransitionSupplierAuditRequest(long ExpectedVersion, string Transition);
public sealed record AnswerSupplierAuditChecklistRequest(long ExpectedVersion, string Status, string Evidence, string Note);
public sealed record AddSupplierAuditFindingRequest(long ExpectedVersion, string Title, string Description, string RequirementReference, string Classification, bool CapaRequired, string Owner, DateTimeOffset ResponseDueAtUtc);
public sealed record RespondSupplierAuditFindingRequest(long ExpectedVersion, string SupplierResponse, string Commitment, DateTimeOffset CommitmentDueAtUtc);
public sealed record SubmitSupplierAuditEvidenceRequest(long ExpectedVersion, string Evidence);
public sealed record CloseSupplierAuditFindingRequest(long ExpectedVersion, string VerificationNote);
public sealed record CreateSupplierInvitationRequest(long ExpectedVersion, string RecipientEmail, DateTimeOffset ExpiresAtUtc);
public sealed record RecordSupplierAuditResultRequest(long ExpectedVersion, string Decision, string Rationale, DateTimeOffset? ValidUntilUtc, bool RequalificationRequired);
public sealed record SupplierAuditListItemResponse(Guid Id, string RecordNumber, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Criticality, int RiskScore, string RiskBand, int RecommendedFrequencyMonths, string QualificationStatus, DateTimeOffset PlannedStartUtc, int FindingCount, int OpenFindingCount, string Status, DateTimeOffset CreatedAtUtc, long Version);
public sealed record SupplierAuditRecordResponse(Guid Id, Guid QualityRecordId, Guid? SupplierEvaluationId, string RecordNumber, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Country, string Criticality, decimal PastPerformanceScore, int OpenFindingSnapshot, int RiskScore, string RiskBand, int RecommendedFrequencyMonths, string Scope, string Site, Guid LeadAuditorUserId, string LeadAuditor, string LeadAuditorDepartment, string PurchasingOwner, DateTimeOffset PlannedStartUtc, DateTimeOffset PlannedEndUtc, string ChecklistVersion, DateTimeOffset? ChecklistLockedAtUtc, string QualificationStatus, string? ResultRationale, DateTimeOffset? QualificationValidUntilUtc, bool RequalificationRequired, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ClosedAtUtc, long Version);
public sealed record SupplierAuditChecklistResponse(Guid Id, int Order, string Category, string Question, string Reference, string Status, string? Evidence, string? Note, DateTimeOffset? AnsweredAtUtc);
public sealed record SupplierAuditFindingResponse(Guid Id, string Number, string Title, string Description, string RequirementReference, string Classification, bool CapaRequired, Guid? LinkedCapaId, string? LinkedCapaNumber, string Owner, DateTimeOffset ResponseDueAtUtc, string? SupplierResponse, string? Commitment, DateTimeOffset? CommitmentDueAtUtc, string? Evidence, string? VerificationNote, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? ClosedAtUtc);
public sealed record SupplierAuditInvitationResponse(Guid Id, string RecipientEmail, DateTimeOffset ExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset? UsedAtUtc);
public sealed record SupplierInvitationCreatedResponse(SupplierAuditDetailsResponse Details, string OneTimeToken, string InvitationPath);
public sealed record SupplierInvitationSubmissionRequest(string Token, Guid FindingId, string SupplierResponse, string Commitment, DateTimeOffset CommitmentDueAtUtc, string Evidence);
public sealed record SupplierInvitationReceiptResponse(string RecordNumber, string FindingNumber, DateTimeOffset AcceptedAtUtc);
public sealed record SupplierAuditEventResponse(Guid Id, long Version, string EventType, string Actor, DateTimeOffset OccurredAtUtc, string? Reason, JsonElement Payload);
public sealed record SupplierAuditTransitionResponse(string Code, string Label);
public sealed record SupplierAuditDetailsResponse(SupplierAuditRecordResponse Record, IReadOnlyList<SupplierAuditChecklistResponse> Checklist, IReadOnlyList<SupplierAuditFindingResponse> Findings, IReadOnlyList<SupplierAuditInvitationResponse> Invitations, IReadOnlyList<SupplierAuditEventResponse> AuditTrail, IReadOnlyList<SupplierAuditTransitionResponse> AvailableTransitions);
