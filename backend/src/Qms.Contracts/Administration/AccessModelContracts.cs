namespace Qms.Contracts.Administration;

public sealed record AccessOverviewResponse(
    IReadOnlyList<DepartmentResponse> Departments,
    IReadOnlyList<PositionResponse> Positions,
    IReadOnlyList<AccessUserResponse> Users,
    IReadOnlyList<DelegationResponse> Delegations,
    IReadOnlyList<RoleDefinitionResponse> Roles,
    IReadOnlyList<WorkflowAssignmentResponse> ActiveAssignments);

public sealed record DepartmentResponse(Guid Id, string Code, string Name, Guid? ManagerUserId, string? ManagerName, bool IsActive);
public sealed record PositionResponse(Guid Id, string Code, string Name, bool IsManagement, bool IsActive);
public sealed record AccessUserResponse(Guid Id, string ProfileKey, string DisplayName, string Email, Guid? DepartmentId, string? DepartmentName, bool IsActive, IReadOnlyList<string> Roles, IReadOnlyList<PositionResponse> Positions);
public sealed record RoleDefinitionResponse(string Code, string Name, string Description);
public sealed record DelegationResponse(Guid Id, Guid DelegatorUserId, string DelegatorName, Guid DelegateUserId, string DelegateName, string Scope, string Reason, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record WorkflowAssignmentResponse(Guid Id, string AggregateType, Guid AggregateId, string TaskRole, Guid AssignedUserId, string AssignedUserName, Guid? AssignedDepartmentId, string? DepartmentName, string Status, DateTimeOffset AssignedAtUtc, DateTimeOffset? DueAtUtc, DateTimeOffset? CompletedAtUtc);

public sealed record UpdateUserAccessRequest(Guid? DepartmentId, IReadOnlyList<string> Roles, IReadOnlyList<Guid> PositionIds, bool IsActive);
public sealed record CreateDelegationRequest(Guid DelegatorUserId, Guid DelegateUserId, string Scope, string Reason, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);
public sealed record CreateWorkflowAssignmentRequest(string AggregateType, Guid AggregateId, string TaskRole, Guid AssignedUserId, Guid? AssignedDepartmentId, DateTimeOffset? DueAtUtc);
