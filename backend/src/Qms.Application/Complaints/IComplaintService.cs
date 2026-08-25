using Qms.Contracts.Common;
using Qms.Contracts.Complaints;

namespace Qms.Application.Complaints;

public interface IComplaintService
{
    Task<PagedResponse<ComplaintListItemResponse>> SearchAsync(ComplaintSearchRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<ComplaintDetailsResponse> CreateAsync(CreateComplaintRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> TransitionAsync(Guid id, TransitionComplaintRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> AddResponseAsync(Guid id, AddComplaintResponseRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> ApproveResponseAsync(Guid id, Guid responseId, ApproveComplaintResponseRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> CompleteInvestigationAsync(Guid id, Guid investigationId, CompleteComplaintInvestigationRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> CompleteImpactAsync(Guid id, CompleteComplaintImpactRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> DecideCapaAsync(Guid id, DecideComplaintCapaRequest request, CancellationToken ct);
}

