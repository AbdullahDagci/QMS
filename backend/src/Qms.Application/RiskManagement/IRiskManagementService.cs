using Qms.Contracts.Common;
using Qms.Contracts.RiskManagement;

namespace Qms.Application.RiskManagement;

public interface IRiskManagementService
{
    Task<PagedResponse<RiskListItemResponse>> SearchAsync(
        RiskSearchRequest r,
        CancellationToken ct
    );
    Task<RiskOptionsResponse> GetOptionsAsync(CancellationToken ct);
    Task<RiskDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<RiskDetailsResponse> CreateAsync(CreateRiskAssessmentRequest r, CancellationToken ct);
    Task<RiskDetailsResponse?> AddItemAsync(Guid id, AddRiskItemRequest r, CancellationToken ct);
    Task<RiskDetailsResponse?> CompleteActionAsync(
        Guid id,
        Guid itemId,
        CompleteRiskActionRequest r,
        CancellationToken ct
    );
    Task<RiskDetailsResponse?> SetResidualAsync(
        Guid id,
        Guid itemId,
        SetResidualRiskRequest r,
        CancellationToken ct
    );
    Task<RiskDetailsResponse?> TransitionAsync(
        Guid id,
        TransitionRiskRequest r,
        CancellationToken ct
    );
    Task<IReadOnlyList<RiskLookupResponse>> ListLookupsAsync(CancellationToken ct);
    Task<RiskLookupResponse> CreateLookupAsync(CreateRiskLookupRequest r, CancellationToken ct);
    Task<RiskLookupResponse?> UpdateLookupAsync(
        Guid id,
        UpdateRiskLookupRequest r,
        CancellationToken ct
    );
}

public sealed record RiskFinalReportFile(byte[] Content, string FileName, string Sha256);

public interface IRiskFinalReportService
{
    Task<RiskFinalReportFile> EnsureGeneratedAsync(RiskDetailsResponse d, CancellationToken ct);
}
