using Qms.Contracts.ChangeControls;
using Qms.Contracts.Common;

namespace Qms.Application.ChangeControls;

public interface IChangeControlService
{
    Task<PagedResponse<ChangeControlListItemResponse>> SearchAsync(ChangeControlSearchRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse> CreateAsync(CreateChangeControlRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> CompleteAssessmentAsync(Guid id, Guid assessmentId, CompleteChangeAssessmentRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> AddActionAsync(Guid id, AddChangeActionRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteChangeActionRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyChangeActionRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> SetAuthorityApprovalAsync(Guid id, SetAuthorityApprovalRequest request, CancellationToken cancellationToken);
    Task<ChangeControlDetailsResponse?> TransitionAsync(Guid id, TransitionChangeControlRequest request, CancellationToken cancellationToken);
}
