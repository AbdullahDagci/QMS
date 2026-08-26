namespace Qms.Contracts.Deviations;
public sealed record DeviationAssignmentRuleResponse(Guid Id, string TaskRole, Guid AssignedUserId, string AssignedUserName, string? DetectedDepartment, string? DeviationType, int? MinimumRiskScore, int Priority, bool IsActive);
public sealed record SaveDeviationAssignmentRuleRequest(string TaskRole, Guid AssignedUserId, string? DetectedDepartment, string? DeviationType, int? MinimumRiskScore, int Priority, bool IsActive = true);
