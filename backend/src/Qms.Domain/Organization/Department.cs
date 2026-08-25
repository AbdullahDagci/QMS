namespace Qms.Domain.Organization;

public sealed class Department
{
    private Department() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentDepartmentId { get; private set; }
    public Guid? ManagerUserId { get; private set; }
    public bool IsActive { get; private set; }

    public static Department Create(string code, string name, Guid? parentDepartmentId = null, Guid? managerUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Department { Id = Guid.CreateVersion7(), Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), ParentDepartmentId = parentDepartmentId, ManagerUserId = managerUserId, IsActive = true };
    }

    public void Update(string name, Guid? parentDepartmentId, Guid? managerUserId, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim(); ParentDepartmentId = parentDepartmentId; ManagerUserId = managerUserId; IsActive = isActive;
    }
}
