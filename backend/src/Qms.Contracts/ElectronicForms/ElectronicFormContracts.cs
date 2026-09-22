using System.Text.Json;

namespace Qms.Contracts.ElectronicForms;

public sealed record CreateElectronicFormRequest(
    string Code,
    string Name,
    string Description,
    string Category,
    string Kind,
    JsonDocument Schema,
    JsonDocument? OutputTemplate,
    string ChangeSummary,
    string WorkflowType);

public sealed record UpdateElectronicFormDraftRequest(
    long DefinitionExpectedVersion,
    long FormVersionExpectedVersion,
    long OutputTemplateExpectedVersion,
    string Name,
    string Description,
    string Category,
    string Kind,
    JsonDocument Schema,
    JsonDocument? OutputTemplate,
    string ChangeSummary,
    string WorkflowType);

public sealed record TransitionElectronicFormVersionRequest(
    long ExpectedVersion,
    string? Password,
    bool MeaningAccepted,
    string? Comment);

public sealed record StartElectronicFormVersionRequest(
    long DefinitionExpectedVersion,
    string ChangeSummary);

public sealed record ElectronicFormListItemResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string Category,
    string Kind,
    bool IsActive,
    int LatestVersionNumber,
    Guid? CurrentPublishedVersionId,
    int? CurrentPublishedVersionNumber,
    string? LatestVersionStatus,
    Guid? LatestVersionId,
    long DefinitionVersion,
    DateTimeOffset UpdatedAtUtc);

public sealed record ElectronicFormVersionResponse(
    Guid Id,
    int VersionNumber,
    string Status,
    int EngineSchemaVersion,
    JsonDocument Schema,
    string ChangeSummary,
    string WorkflowType,
    Guid CreatedByUserId,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    Guid? PublishedByUserId,
    string? PublishedBy,
    DateTimeOffset? PublishedAtUtc,
    long RowVersion,
    Guid OutputTemplateId,
    JsonDocument OutputTemplate,
    long OutputTemplateRowVersion);

public sealed record ElectronicFormDetailsResponse(
    ElectronicFormListItemResponse Definition,
    IReadOnlyList<ElectronicFormVersionResponse> Versions);

public sealed record CreateElectronicFormRecordRequest(
    Guid FormDefinitionId,
    JsonDocument Data);

public sealed record UpdateElectronicFormRecordRequest(
    long ExpectedVersion,
    JsonDocument Data);

public sealed record SubmitElectronicFormRecordRequest(long ExpectedVersion);

public sealed record ApproveElectronicFormRecordRequest(
    long ExpectedVersion,
    string Password,
    bool MeaningAccepted,
    string? Comment);

public sealed record ElectronicFormRecordListItemResponse(
    Guid Id,
    Guid QualityRecordId,
    string RecordNumber,
    string FormCode,
    string FormName,
    int FormVersionNumber,
    string Status,
    string CreatedBy,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record ElectronicFormRecordAuditResponse(
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    long Version,
    string? Reason);

public sealed record ElectronicFormRecordSignatureResponse(
    Guid Id,
    string Meaning,
    string Signer,
    DateTimeOffset SignedAtUtc,
    long RecordVersion,
    string ContentHash,
    string? Comment);

public sealed record ElectronicFormRecordDetailsResponse(
    ElectronicFormRecordListItemResponse Record,
    Guid FormDefinitionId,
    Guid FormVersionId,
    Guid OutputTemplateId,
    JsonDocument Schema,
    JsonDocument OutputTemplate,
    JsonDocument Data,
    IReadOnlyList<ElectronicFormRecordAuditResponse> AuditTrail,
    IReadOnlyList<ElectronicFormRecordSignatureResponse> Signatures);

public sealed record ElectronicFormFinalReportFile(byte[] Content, string FileName, string Sha256);
