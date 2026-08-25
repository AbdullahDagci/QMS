using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Qms.Api.Security;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    QmsDbContext dbContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "QmsDevelopment";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var profileKey = Request.Headers["X-QMS-Profile"].FirstOrDefault()?.Trim().ToLowerInvariant() ?? "quality";
        var fallback = DevelopmentProfiles.Resolve(profileKey);
        ApplicationUser? stored = null;
        IReadOnlyList<string> roles = fallback.Roles;
        try
        {
            stored = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.ProfileKey == profileKey, Context.RequestAborted);
            if (stored is not null)
            {
                roles = await (from userRole in dbContext.UserRoles.AsNoTracking()
                               join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                               where userRole.UserId == stored.Id
                               select role.Name!).ToListAsync(Context.RequestAborted);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "Geliştirme kullanıcı profilleri veritabanından okunamadı; yerleşik profil kullanılıyor.");
        }
        var userId = stored?.Id ?? fallback.UserId;
        var displayName = stored?.DisplayName ?? fallback.DisplayName;
        var departmentId = stored?.DepartmentId ?? fallback.DepartmentId;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
            new("qms_profile", profileKey),
            new("qms_department", departmentId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
