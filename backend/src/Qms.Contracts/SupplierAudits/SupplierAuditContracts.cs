using System.Text.Json;
using Qms.Contracts.Common;

namespace Qms.Contracts.SupplierAudits;

public sealed record SupplierAuditCodeOption(string Code, string Name);
public sealed record SupplierAuditIdentityOption(Guid Id, string Name, string? Department);
public sealed record SupplierAuditOptionsResponse(IReadOnlyList<SupplierAuditIdentityOption> Users, IReadOnlyList<SupplierAuditCodeOption> Countries, IReadOnlyList<SupplierAuditCodeOption> Criticalities, IReadOnlyList<SupplierAuditCodeOption> FindingClassifications);
public sealed record SupplierAuditLookupDefinitionResponse(Guid Id, string Category, string Code, string Name, int SortOrder, bool IsActive);
public sealed record CreateSupplierAuditLookupDefinitionRequest(string Category, string Code, string Name, int SortOrder);
public sealed record UpdateSupplierAuditLookupDefinitionRequest(string Name, int SortOrder, bool IsActive);

public sealed record SupplierAuditSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record SupplierAuditChecklistInput(string Category, string Question, string Reference);
public sealed record CreateSupplierAuditRequest(Guid? SupplierEvaluationId, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Country, string Criticality, decimal PastPerformanceScore, int OpenFindingCount, string Scope, string Site, Guid LeadAuditorUserId, string LeadAuditor, string LeadAuditorDepartment, Guid PurchasingOwnerUserId, string PurchasingOwner, Guid VerifierUserId, Guid QualityApproverUserId, DateTimeOffset PlannedStartUtc, DateTimeOffset PlannedEndUtc, string ChecklistVersion, IReadOnlyList<SupplierAuditChecklistInput> Checklist);
public sealed record TransitionSupplierAuditRequest(long ExpectedVersion, string Transition, string? Note = null, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
public sealed record AnswerSupplierAuditChecklistRequest(long ExpectedVersion, string Status, string Evidence, string Note);
public sealed record AddSupplierAuditFindingRequest(long ExpectedVersion, string Title, string Description, string RequirementReference, string Classification, bool CapaRequired, Guid OwnerUserId, string Owner, DateTimeOffset ResponseDueAtUtc);
public sealed record RespondSupplierAuditFindingRequest(long ExpectedVersion, string SupplierResponse, string Commitment, DateTimeOffset CommitmentDueAtUtc);
public sealed record SubmitSupplierAuditEvidenceRequest(long ExpectedVersion, string Evidence);
public sealed record CloseSupplierAuditFindingRequest(long ExpectedVersion, string VerificationNote, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
public sealed record CreateSupplierInvitationRequest(long ExpectedVersion, string RecipientEmail, DateTimeOffset ExpiresAtUtc);
public sealed record RecordSupplierAuditResultRequest(long ExpectedVersion, string Decision, string Rationale, DateTimeOffset? ValidUntilUtc, bool RequalificationRequired, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
public sealed record SupplierAuditListItemResponse(Guid Id, string RecordNumber, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Criticality, int RiskScore, string RiskBand, int RecommendedFrequencyMonths, string QualificationStatus, DateTimeOffset PlannedStartUtc, int FindingCount, int OpenFindingCount, string Status, DateTimeOffset CreatedAtUtc, long Version);
public sealed record SupplierAuditRecordResponse(Guid Id, Guid QualityRecordId, Guid? SupplierEvaluationId, string RecordNumber, string SupplierCode, string SupplierName, string SupplierScope, string MaterialOrService, string Country, string Criticality, decimal PastPerformanceScore, int OpenFindingSnapshot, int RiskScore, string RiskBand, int RecommendedFrequencyMonths, string Scope, string Site, Guid LeadAuditorUserId, string LeadAuditor, string LeadAuditorDepartment, Guid PurchasingOwnerUserId, string PurchasingOwner, Guid VerifierUserId, string Verifier, Guid QualityApproverUserId, string QualityApprover, DateTimeOffset PlannedStartUtc, DateTimeOffset PlannedEndUtc, string ChecklistVersion, DateTimeOffset? ChecklistLockedAtUtc, string QualificationStatus, string? ResultRationale, DateTimeOffset? QualificationValidUntilUtc, bool RequalificationRequired, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ClosedAtUtc, long Version);
public sealed record SupplierAuditChecklistResponse(Guid Id, int Order, string Category, string Question, string Reference, string Status, string? Evidence, string? Note, DateTimeOffset? AnsweredAtUtc);
public sealed record SupplierAuditFindingResponse(Guid Id, string Number, string Title, string Description, string RequirementReference, string Classification, bool CapaRequired, Guid? LinkedCapaId, string? LinkedCapaNumber, Guid OwnerUserId, string Owner, DateTimeOffset ResponseDueAtUtc, string? SupplierResponse, string? Commitment, DateTimeOffset? CommitmentDueAtUtc, string? Evidence, string? VerificationNote, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? ClosedAtUtc);
public sealed record SupplierAuditInvitationResponse(Guid Id, string RecipientEmail, DateTimeOffset ExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset? UsedAtUtc);
public sealed record SupplierInvitationCreatedResponse(SupplierAuditDetailsResponse Details, string OneTimeToken, string InvitationPath);
public sealed record SupplierInvitationSubmissionRequest(string Token, Guid FindingId, string SupplierResponse, string Commitment, DateTimeOffset CommitmentDueAtUtc, string Evidence);
public sealed record SupplierInvitationReceiptResponse(string RecordNumber, string FindingNumber, DateTimeOffset AcceptedAtUtc);
public sealed record SupplierAuditEventResponse(Guid Id, long Version, string EventType, string Actor, DateTimeOffset OccurredAtUtc, string? Reason, JsonElement Payload);
public sealed record SupplierAuditTransitionResponse(string Code, string Label);
public sealed record SupplierAuditSignatureResponse(Guid Id, long RecordVersion, Guid SignedByUserId, string SignedBy, string Meaning, DateTimeOffset SignedAtUtc, string Hash, string? Reason);
public sealed record SupplierAuditDetailsResponse(SupplierAuditRecordResponse Record, IReadOnlyList<SupplierAuditChecklistResponse> Checklist, IReadOnlyList<SupplierAuditFindingResponse> Findings, IReadOnlyList<SupplierAuditInvitationResponse> Invitations, IReadOnlyList<SupplierAuditEventResponse> AuditTrail, IReadOnlyList<SupplierAuditTransitionResponse> AvailableTransitions, IReadOnlyList<SupplierAuditSignatureResponse> Signatures);
