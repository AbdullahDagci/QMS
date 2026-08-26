using Qms.Contracts.Common;
using Qms.Contracts.SpecializedRecords;

namespace Qms.Application.SpecializedRecords;

public interface ISpecializedRecordService
{
    Task<PagedResponse<SpecializedListItemResponse>> SearchAsync(
        string module,
        SpecializedSearchRequest request,
        CancellationToken ct
    );
    Task<SpecializedOptionsResponse> GetOptionsAsync(string module, CancellationToken ct);
    Task<SpecializedDetailsResponse?> GetDetailsAsync(string module, Guid id, CancellationToken ct);
    Task<SpecializedDetailsResponse> CreateAsync(
        string module,
        CreateSpecializedRecordRequest request,
        CancellationToken ct
    );
    Task<SpecializedDetailsResponse?> TransitionAsync(
        string module,
        Guid id,
        TransitionSpecializedRecordRequest request,
        CancellationToken ct
    );
    Task<IReadOnlyList<SpecializedLookupResponse>> ListLookupsAsync(
        string module,
        CancellationToken ct
    );
    Task<SpecializedLookupResponse> CreateLookupAsync(
        string module,
        CreateSpecializedLookupRequest request,
        CancellationToken ct
    );
    Task<SpecializedLookupResponse?> UpdateLookupAsync(
        string module,
        Guid id,
        UpdateSpecializedLookupRequest request,
        CancellationToken ct
    );
}

public sealed record SpecializedFinalReportFile(byte[] Content, string FileName, string Sha256);

public interface ISpecializedFinalReportService
{
    Task<SpecializedFinalReportFile> EnsureGeneratedAsync(
        SpecializedDetailsResponse details,
        CancellationToken ct
    );
}
