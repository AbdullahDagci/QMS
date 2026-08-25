using Microsoft.AspNetCore.Identity;

namespace Qms.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public string? ProfileKey { get; set; }

    public Guid? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}
