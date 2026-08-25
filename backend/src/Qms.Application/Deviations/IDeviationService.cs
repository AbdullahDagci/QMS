using Qms.Contracts.Deviations;
using Qms.Contracts.Common;

namespace Qms.Application.Deviations;

public interface IDeviationService
{
    Task<IReadOnlyList<DeviationListItemResponse>> ListAsync(CancellationToken cancellationToken);

    Task<PagedResponse<DeviationListItemResponse>> SearchAsync(
        DeviationSearchRequest request,
        CancellationToken cancellationToken);

    Task<DeviationResponse?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<DeviationDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<DeviationResponse> CreateAsync(
        CreateDeviationRequest request,
        CancellationToken cancellationToken);

    Task<DeviationResponse?> SubmitAsync(
        Guid id,
        SubmitDeviationRequest request,
        CancellationToken cancellationToken);

    Task<DeviationDetailsResponse?> AddInvestigationAsync(
        Guid id,
        AddDeviationInvestigationRequest request,
        CancellationToken cancellationToken);

    Task<DeviationDetailsResponse?> AddBatchImpactAsync(
        Guid id,
        AddDeviationBatchImpactRequest request,
        CancellationToken cancellationToken);

    Task<DeviationDetailsResponse?> TransitionAsync(
        Guid id,
        TransitionDeviationRequest request,
        CancellationToken cancellationToken);
}
