using System.Security.Claims;
using Qms.Application.Security;

namespace Qms.Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("Etkin kullanıcı bağlamı bulunamadı.");

    public Guid Id => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("Etkin kullanıcı kimliği geçersiz.");

    public string DisplayName => User.Identity?.Name ?? "Bilinmeyen kullanıcı";

    public Guid? DepartmentId => Guid.TryParse(User.FindFirstValue("qms_department"), out var id) ? id : null;

    public IReadOnlySet<string> Roles => User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);

    public bool IsInRole(string role) => User.IsInRole(role);
}
