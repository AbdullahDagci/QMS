using System.Text.Json;

namespace Qms.Contracts.Deviations;

public sealed record DeviationDetailsResponse(
    DeviationResponse Record,
    IReadOnlyList<DeviationInvestigationResponse> Investigations,
    IReadOnlyList<DeviationBatchImpactResponse> BatchImpacts,
    IReadOnlyList<DeviationLinkedCapaResponse> LinkedCapas,
    IReadOnlyList<DeviationAuditEventResponse> AuditTrail,
    IReadOnlyList<DeviationSignatureResponse> Signatures,
    IReadOnlyList<DeviationTransitionResponse> AvailableTransitions,
    bool CanAddInvestigation,
    bool CanAddBatchImpact,
    DeviationNextAssigneeResponse? NextAssignee = null);

// Kayıt mevcut adımdan ilerletildiğinde görevin atama matrisine göre kime düşeceği; UserId boşsa kural yoktur.
public sealed record DeviationNextAssigneeResponse(
    string TaskRole,
    Guid? UserId,
    string? UserName,
    string? DepartmentName);

public sealed record DeviationSignatureResponse(Guid Id, long RecordVersion, Guid SignerUserId, string SignerName, string Meaning, DateTimeOffset SignedAtUtc, string ContentHash, string? Comment);

public sealed record DeviationInvestigationResponse(
    Guid Id,
    string Method,
    string RootCauseCategory,
    string RootCauseDescription,
    string Conclusion,
    Guid InvestigatorUserId,
    string InvestigatorName,
    string InvestigatorDepartment,
    DateTimeOffset CompletedAtUtc);

public sealed record DeviationBatchImpactResponse(
    Guid Id,
    string BatchNumber,
    bool IsAffected,
    bool IsLocked,
    string Disposition,
    string Rationale,
    Guid AssessedByUserId,
    string AssessedByName,
    string AssessedByDepartment,
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
