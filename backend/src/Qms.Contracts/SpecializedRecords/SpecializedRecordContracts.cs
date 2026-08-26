using System.Text.Json;
using Qms.Contracts.Common;

namespace Qms.Contracts.SpecializedRecords;

public sealed record SpecializedSearchRequest(
    int Page = 1,
    int PageSize = 25,
    string SortBy = "createdAtUtc",
    string SortDirection = "desc",
    IReadOnlyList<ColumnFilterRequest>? Filters = null
);

public sealed record SpecializedUserOption(Guid Id, string Name, string? Department);

public sealed record SpecializedCodeOption(string Code, string Name);

public sealed record SpecializedOptionsResponse(
    IReadOnlyList<SpecializedUserOption> Users,
    IReadOnlyList<SpecializedCodeOption> Types,
    IReadOnlyList<SpecializedCodeOption> Subjects,
    IReadOnlyList<SpecializedCodeOption> Scopes
);

public sealed record CreateSpecializedRecordRequest(
    string Title,
    string TypeCode,
    string SubjectCode,
    string ScopeCode,
    string Reference,
    string Description,
    JsonElement StructuredData,
    DateTimeOffset DueAtUtc,
    Guid OwnerUserId,
    Guid ReviewerUserId,
    Guid ApproverUserId
);

public sealed record TransitionSpecializedRecordRequest(
    long ExpectedVersion,
    string Transition,
    string? SignaturePassword = null,
    bool SignatureMeaningAccepted = false,
    string? Note = null
);

public sealed record SpecializedListItemResponse(
    Guid Id,
    string RecordNumber,
    string Title,
    string TypeName,
    string SubjectName,
    string ScopeName,
    string Owner,
    string Status,
    DateTimeOffset DueAtUtc,
    DateTimeOffset CreatedAtUtc,
    long Version
);

public sealed record SpecializedRecordResponse(
    Guid Id,
    Guid QualityRecordId,
    string ModuleCode,
    string RecordNumber,
    string Title,
    string TypeCode,
    string TypeName,
    string SubjectCode,
    string SubjectName,
    string ScopeCode,
    string ScopeName,
    string Reference,
    string Description,
    JsonElement StructuredData,
    DateTimeOffset DueAtUtc,
    Guid OwnerUserId,
    string Owner,
    Guid ReviewerUserId,
    string Reviewer,
    Guid ApproverUserId,
    string Approver,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version
);

public sealed record SpecializedEventResponse(
    Guid Id,
    long Version,
    string EventType,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    string? Reason,
    JsonElement Payload
);

public sealed record SpecializedSignatureResponse(
    Guid Id,
    long RecordVersion,
    string Signer,
    string Meaning,
    DateTimeOffset SignedAtUtc,
    string ContentHash,
    string? Comment
);

public sealed record SpecializedTransitionResponse(
    string Code,
    string Label,
    bool RequiresSignature = false
);

public sealed record SpecializedDetailsResponse(
    SpecializedRecordResponse Record,
    IReadOnlyList<SpecializedEventResponse> AuditTrail,
    IReadOnlyList<SpecializedSignatureResponse> Signatures,
    IReadOnlyList<SpecializedTransitionResponse> AvailableTransitions
);

public sealed record SpecializedLookupResponse(
    Guid Id,
    string ModuleCode,
    string Category,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive
);

public sealed record CreateSpecializedLookupRequest(
    string Category,
    string Code,
    string Name,
    int SortOrder
);

public sealed record UpdateSpecializedLookupRequest(string Name, int SortOrder, bool IsActive);
