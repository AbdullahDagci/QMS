using System.Text.Json;

namespace Qms.Contracts.Capas;

public sealed record CapaListItemResponse(Guid Id, string RecordNumber, Guid? SourceDeviationId, string? SourceRecordNumber, string SourceType, string Title, string Owner, DateTimeOffset TargetDateUtc, string Status, int ActionCount, int VerifiedActionCount, DateTimeOffset CreatedAtUtc, long Version);
public sealed record CapaResponse(Guid Id, Guid QualityRecordId, string RecordNumber, Guid? SourceDeviationId, string? SourceRecordNumber, string SourceType, string Title, string Description, string RootCause, string ImmediateActions, string Owner, DateTimeOffset TargetDateUtc, bool EffectivenessRequired, string EffectivenessMethod, string EffectivenessSample, int ObservationPeriodDays, string SuccessCriteria, string EffectivenessEvaluator, DateTimeOffset? EffectivenessDueDateUtc, bool? IsEffective, string? EffectivenessResult, string? ClosureNote, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, DateTimeOffset? ClosedAtUtc, long Version);
public sealed record CapaActionResponse(Guid Id, string ActionType, string Description, string Owner, DateTimeOffset TargetDateUtc, string Status, string? CompletionEvidence, string? VerificationNote, DateTimeOffset? CompletedAtUtc, DateTimeOffset? VerifiedAtUtc);
public sealed record CapaAuditEventResponse(Guid Id, long Version, string EventType, string Actor, DateTimeOffset OccurredAtUtc, string? Reason, JsonElement Payload);
public sealed record CapaTransitionResponse(string Code, string Label, bool NoteRequired);
public sealed record CapaDetailsResponse(CapaResponse Record, IReadOnlyList<CapaActionResponse> Actions, IReadOnlyList<CapaAuditEventResponse> AuditTrail, IReadOnlyList<CapaTransitionResponse> AvailableTransitions);
