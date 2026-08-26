using Qms.Contracts.Deviations;
using Qms.Contracts.Common;

namespace Qms.Application.Deviations;

public interface IDeviationService
{
    Task<DeviationLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviationTypeResponse>> ListDeviationTypesAsync(CancellationToken cancellationToken);
    Task<DeviationTypeResponse> CreateDeviationTypeAsync(CreateDeviationTypeRequest request, CancellationToken cancellationToken);
    Task<DeviationTypeResponse?> UpdateDeviationTypeAsync(Guid id, UpdateDeviationTypeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviationAssignmentRuleResponse>> ListAssignmentRulesAsync(CancellationToken cancellationToken);
    Task<DeviationAssignmentRuleResponse> CreateAssignmentRuleAsync(SaveDeviationAssignmentRuleRequest request, CancellationToken cancellationToken);
    Task<DeviationAssignmentRuleResponse?> UpdateAssignmentRuleAsync(Guid id, SaveDeviationAssignmentRuleRequest request, CancellationToken cancellationToken);

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

public sealed record DeviationFinalReportFile(byte[] Content, string FileName, string Sha256);

public interface IDeviationFinalReportService
{
    Task<DeviationFinalReportFile> EnsureGeneratedAsync(
        DeviationDetailsResponse details,
        CancellationToken cancellationToken);
}
