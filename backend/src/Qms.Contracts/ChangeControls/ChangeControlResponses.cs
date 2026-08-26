using System.Text.Json;

namespace Qms.Contracts.ChangeControls;

public sealed record ChangeControlListItemResponse(Guid Id, string RecordNumber, Guid? SourceCapaId, string? SourceRecordNumber, string ChangeType, string Title, string Owner, DateTimeOffset TargetDateUtc, string RiskLevel, string RegulatoryImpact, string Status, int AssessmentCount, int CompletedAssessmentCount, int ActionCount, int VerifiedActionCount, DateTimeOffset CreatedAtUtc, long Version);
public sealed record ChangeControlResponse(Guid Id, Guid QualityRecordId, string RecordNumber, Guid? SourceCapaId, string? SourceRecordNumber, string ChangeType, string Title, string CurrentState, string ProposedState, string Justification, string Scope, bool IsTemporary, DateTimeOffset? TemporaryUntilUtc, Guid? OwnerUserId, string Owner, DateTimeOffset TargetDateUtc, string RiskLevel, string RiskSummary, bool ProductImpact, bool SiteImpact, bool ValidationRequired, string RegulatoryImpact, string RollbackPlan, string? AuthorityApprovalReference, DateTimeOffset? CommissionedAtUtc, string? PostImplementationResult, string? ClosureNote, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ClosedAtUtc, long Version);
public sealed record ChangeAssessmentResponse(Guid Id, Guid? DepartmentId, string Department, Guid? ReviewerUserId, string Reviewer, string Status, string? ImpactSummary, string? RequiredActions, DateTimeOffset? CompletedAtUtc);
public sealed record ChangeActionResponse(Guid Id, string Category, string Description, Guid? OwnerUserId, string Owner, DateTimeOffset TargetDateUtc, bool IsBlocking, string Status, string? CompletionEvidence, string? VerificationNote, DateTimeOffset? CompletedAtUtc, DateTimeOffset? VerifiedAtUtc);
public sealed record ChangeAuditEventResponse(Guid Id, long Version, string EventType, string Actor, DateTimeOffset OccurredAtUtc, string? Reason, JsonElement Payload);
public sealed record ChangeTransitionResponse(string Code, string Label, bool NoteRequired);
public sealed record ChangeSignatureResponse(Guid Id, long RecordVersion, Guid SignerUserId, string SignerName, string Meaning, DateTimeOffset SignedAtUtc, string ContentHash, string? Comment);
public sealed record ChangeUserOptionResponse(Guid Id, string DisplayName, string? DepartmentName);
public sealed record ChangeDepartmentOptionResponse(Guid Id, string Code, string Name, Guid ReviewerUserId, string ReviewerName);
public sealed record ChangeLookupDefinitionResponse(Guid Id, string Category, string Code, string Name, int SortOrder, bool IsActive);
public sealed record ChangeLookupOptionResponse(string Code, string Name);
public sealed record CreateChangeLookupDefinitionRequest(string Category, string Code, string Name, int SortOrder);
public sealed record UpdateChangeLookupDefinitionRequest(string Name, int SortOrder, bool IsActive);
public sealed record ChangeControlLookupsResponse(IReadOnlyList<ChangeUserOptionResponse> Owners, IReadOnlyList<ChangeDepartmentOptionResponse> Departments, IReadOnlyList<ChangeLookupOptionResponse> ChangeTypes, IReadOnlyList<ChangeLookupOptionResponse> RiskLevels, IReadOnlyList<ChangeLookupOptionResponse> RegulatoryImpacts, IReadOnlyList<ChangeLookupOptionResponse> ActionCategories);
public sealed record ChangeControlDetailsResponse(ChangeControlResponse Record, IReadOnlyList<ChangeAssessmentResponse> Assessments, IReadOnlyList<ChangeActionResponse> Actions, IReadOnlyList<ChangeAuditEventResponse> AuditTrail, IReadOnlyList<ChangeSignatureResponse> Signatures, IReadOnlyList<ChangeTransitionResponse> AvailableTransitions, IReadOnlyList<string> ActionableTaskRoles);
