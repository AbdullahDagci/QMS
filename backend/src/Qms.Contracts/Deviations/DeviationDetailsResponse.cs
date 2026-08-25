using System.Text.Json;

namespace Qms.Contracts.Deviations;

public sealed record DeviationDetailsResponse(
    DeviationResponse Record,
    IReadOnlyList<DeviationInvestigationResponse> Investigations,
    IReadOnlyList<DeviationBatchImpactResponse> BatchImpacts,
    IReadOnlyList<DeviationLinkedCapaResponse> LinkedCapas,
    IReadOnlyList<DeviationAuditEventResponse> AuditTrail,
    IReadOnlyList<DeviationTransitionResponse> AvailableTransitions);

public sealed record DeviationInvestigationResponse(
    Guid Id,
    string Method,
    string RootCauseCategory,
    string RootCauseDescription,
    string Conclusion,
    DateTimeOffset CompletedAtUtc);

public sealed record DeviationBatchImpactResponse(
    Guid Id,
    string BatchNumber,
    bool IsAffected,
    bool IsLocked,
    string Disposition,
    string Rationale,
    DateTimeOffset AssessedAtUtc);

public sealed record DeviationAuditEventResponse(
    Guid Id,
    long Version,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    string? Reason,
    JsonElement Payload);

public sealed record DeviationTransitionResponse(
    string Code,
    string Label,
    bool NoteRequired);

public sealed record DeviationLinkedCapaResponse(
    Guid Id,
    string RecordNumber,
    string Title,
    string Owner,
    string Status,
    DateTimeOffset TargetDateUtc);
