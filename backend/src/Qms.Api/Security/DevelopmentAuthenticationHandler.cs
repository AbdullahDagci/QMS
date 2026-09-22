using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Qms.Api.Security;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    QmsDbContext dbContext,
    IWebHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "QmsSession";
    public const string SessionCookieName = "__Host-qms-session";
    public const string DevelopmentSessionCookieName = "qms-session-dev";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.FirstOrDefault();
        var raw = authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authorization[7..].Trim()
            : Request.Cookies[SessionCookieName] ?? Request.Cookies[DevelopmentSessionCookieName];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
            var now = DateTimeOffset.UtcNow;
            var sessionUser = await (from session in dbContext.UserSessions.AsNoTracking()
                                     join user in dbContext.Users.AsNoTracking() on session.UserId equals user.Id
                                     where session.TokenHash == hash && session.RevokedAtUtc == null && session.ExpiresAtUtc > now && user.IsActive
                                     select user).SingleOrDefaultAsync(Context.RequestAborted);
            if (sessionUser is null) return AuthenticateResult.Fail("Geçersiz veya süresi dolmuş oturum.");
            var sessionRoles = await (from ur in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on ur.RoleId equals role.Id where ur.UserId == sessionUser.Id select role.Name!).ToListAsync(Context.RequestAborted);
            return Success(sessionUser, sessionUser.ProfileKey ?? sessionUser.Id.ToString(), sessionRoles);
        }

        if (!environment.IsDevelopment())
            return AuthenticateResult.NoResult();

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
        var fallbackUser = stored ?? new ApplicationUser { Id = fallback.UserId, DisplayName = fallback.DisplayName, DepartmentId = fallback.DepartmentId };
        return Success(fallbackUser, profileKey, roles);
    }

    private AuthenticateResult Success(ApplicationUser user, string profileKey, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("qms_profile", profileKey),
            new("qms_department", user.DepartmentId?.ToString() ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
