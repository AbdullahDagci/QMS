using Qms.Contracts.Administration;

namespace Qms.Application.Administration;

public interface IAccessAdministrationService
{
    Task<AccessOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
    Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken);
    Task<DepartmentResponse?> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken);
    Task<AccessUserResponse?> UpdateUserAsync(Guid userId, UpdateUserAccessRequest request, CancellationToken cancellationToken);
    Task<AccessUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<DelegationResponse> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken cancellationToken);
    Task<bool> RevokeDelegationAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowAssignmentResponse>> GetAssignmentsAsync(string aggregateType, Guid aggregateId, CancellationToken cancellationToken);
    Task<WorkflowAssignmentResponse> CreateAssignmentAsync(CreateWorkflowAssignmentRequest request, CancellationToken cancellationToken);
}
