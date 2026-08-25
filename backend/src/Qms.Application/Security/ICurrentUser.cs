namespace Qms.Application.Security;

public interface ICurrentUser
{
    Guid Id { get; }
    string DisplayName { get; }
    Guid? DepartmentId { get; }
    IReadOnlySet<string> Roles { get; }
    bool IsInRole(string role);
}
