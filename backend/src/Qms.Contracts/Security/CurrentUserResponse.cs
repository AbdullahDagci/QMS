namespace Qms.Contracts.Security;

public sealed record CurrentUserResponse(
    string Id,
    string DisplayName,
    string Profile,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<DevelopmentProfileResponse> AvailableProfiles);

public sealed record DevelopmentProfileResponse(string Key, string DisplayName, string DepartmentName, IReadOnlyList<string> Roles);
