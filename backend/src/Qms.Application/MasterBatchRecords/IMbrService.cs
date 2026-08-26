using Qms.Contracts.Common;
using Qms.Contracts.MasterBatchRecords;

namespace Qms.Application.MasterBatchRecords;

public interface IMbrService
{
    Task<PagedResponse<MbrListItemResponse>> SearchAsync(MbrSearchRequest r, CancellationToken ct);
    Task<MbrOptionsResponse> GetOptionsAsync(CancellationToken ct);
    Task<MbrDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<MbrDetailsResponse> CreateAsync(CreateMbrRequest r, CancellationToken ct);
    Task<MbrDetailsResponse?> AddStepAsync(Guid id, AddMbrStepRequest r, CancellationToken ct);
    Task<MbrDetailsResponse?> TransitionAsync(
        Guid id,
        TransitionMbrRequest r,
        CancellationToken ct
    );
    Task<IReadOnlyList<MbrLookupResponse>> ListLookupsAsync(CancellationToken ct);
    Task<MbrLookupResponse> CreateLookupAsync(CreateMbrLookupRequest r, CancellationToken ct);
    Task<MbrLookupResponse?> UpdateLookupAsync(
        Guid id,
        UpdateMbrLookupRequest r,
        CancellationToken ct
    );
}

public sealed record MbrFinalReportFile(byte[] Content, string FileName, string Sha256);

public interface IMbrFinalReportService
{
    Task<MbrFinalReportFile> EnsureGeneratedAsync(MbrDetailsResponse details, CancellationToken ct);
}
