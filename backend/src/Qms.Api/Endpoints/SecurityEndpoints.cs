using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Qms.Application.Security;
using Qms.Contracts.Security;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Qms.Domain.Identity;
using Qms.Domain.AuditTrail;
using Qms.Api.Security;
using System.Text.Json;

namespace Qms.Api.Endpoints;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/auth/login", async (LoginRequest request, UserManager<ApplicationUser> userManager, QmsDbContext db, TimeProvider clock, HttpContext context, IWebHostEnvironment environment, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.Unauthorized();
            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user)) return Results.Unauthorized();
            if (!await userManager.CheckPasswordAsync(user, request.Password))
            {
                await userManager.AccessFailedAsync(user);
                return Results.Unauthorized();
            }
            await userManager.ResetAccessFailedCountAsync(user);
            return await CreateSession(user, "Password", db, clock, context, environment, ct);
        }).AllowAnonymous().RequireRateLimiting("authentication").WithTags("Security");

        endpoints.MapPost("/api/v1/auth/quick-login", async (QuickLoginRequest request, IWebHostEnvironment environment, QmsDbContext db, TimeProvider clock, HttpContext context, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment()) return Results.NotFound();
            var user = await db.Users.SingleOrDefaultAsync(x => x.ProfileKey == request.ProfileKey && x.IsActive, ct);
            return user is null ? Results.NotFound() : await CreateSession(user, "DevelopmentQuickProfile", db, clock, context, environment, ct);
        }).AllowAnonymous().RequireRateLimiting("authentication").WithTags("Security");
        endpoints.MapGet("/api/v1/auth/quick-profiles", (IWebHostEnvironment environment) => environment.IsDevelopment()
            ? Results.Ok(DevelopmentProfiles.All.Select(x => new DevelopmentProfileResponse(x.Key, x.DisplayName, x.DepartmentName, x.Roles)))
            : Results.NotFound()).AllowAnonymous().WithTags("Security");

        endpoints.MapPost("/api/v1/auth/logout", async (HttpContext context, QmsDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var raw = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                ?? context.Request.Cookies[DevelopmentAuthenticationHandler.SessionCookieName]
                ?? context.Request.Cookies[DevelopmentAuthenticationHandler.DevelopmentSessionCookieName];
            if (!string.IsNullOrWhiteSpace(raw)) { var hash = Hash(raw); var session = await db.UserSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, ct); if (session is not null) { var now = clock.GetUtcNow(); session.Revoke(now); db.AuditEvents.Add(AuditEvent.Create("UserSession", session.Id, 2, "UserLoggedOut", session.UserId, context.User.Identity?.Name ?? "QMS kullanıcısı", now, Qms.Infrastructure.Integrity.AuditCorrelation.Current, JsonSerializer.SerializeToDocument(new { session.UserId }))); await db.SaveChangesAsync(ct); } }
            context.Response.Cookies.Delete(DevelopmentAuthenticationHandler.SessionCookieName, new CookieOptions { Path = "/", Secure = true });
            context.Response.Cookies.Delete(DevelopmentAuthenticationHandler.DevelopmentSessionCookieName, new CookieOptions { Path = "/" });
            return Results.NoContent();
        }).RequireAuthorization().WithTags("Security");

        endpoints.MapPost("/api/v1/auth/change-password", async (ChangePasswordRequest request,
            UserManager<ApplicationUser> userManager, QmsDbContext db, TimeProvider clock,
            HttpContext context, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = ["Mevcut ve yeni parola zorunludur."]
                });
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId ?? string.Empty);
            if (user is null) return Results.Unauthorized();
            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword,
                request.NewPassword);
            if (!result.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = result.Errors.Select(error => error.Description).ToArray()
                });

            var now = clock.GetUtcNow();
            var raw = context.Request.Cookies[DevelopmentAuthenticationHandler.SessionCookieName]
                ?? context.Request.Cookies[DevelopmentAuthenticationHandler.DevelopmentSessionCookieName];
            var currentHash = string.IsNullOrWhiteSpace(raw) ? null : Hash(raw);
            var otherSessions = await db.UserSessions.Where(session => session.UserId == user.Id
                && session.RevokedAtUtc == null && session.ExpiresAtUtc > now
                && (currentHash == null || session.TokenHash != currentHash)).ToListAsync(ct);
            foreach (var session in otherSessions) session.Revoke(now);
            db.AuditEvents.Add(AuditEvent.Create("UserSecurity", user.Id, now.UtcTicks,
                "UserPasswordChanged", user.Id, user.DisplayName, now,
                Qms.Infrastructure.Integrity.AuditCorrelation.Current,
                JsonSerializer.SerializeToDocument(new { revokedSessionCount = otherSessions.Count })));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("authentication").WithTags("Security");

        endpoints.MapGet("/api/v1/auth/me", async (
                ClaimsPrincipal user,
                IAuthorizationService authorizationService,
                IWebHostEnvironment environment) =>
            {
                var permissions = new List<string>();
                foreach (var policy in QmsPolicies.All)
                {
                    if ((await authorizationService.AuthorizeAsync(user, policy)).Succeeded)
                    {
                        permissions.Add(policy);
                    }
                }

                return Results.Ok(new CurrentUserResponse(
                    user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                    user.Identity?.Name ?? "Bilinmeyen kullanıcı",
                    user.FindFirstValue("qms_profile") ?? string.Empty,
                    user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList(),
                    permissions,
                    (environment.IsDevelopment() ? DevelopmentProfiles.All : []).Select(profile => new DevelopmentProfileResponse(
                        profile.Key,
                        profile.DisplayName,
                        profile.DepartmentName,
                        profile.Roles)).ToList()));
            })
            .RequireAuthorization()
            .WithTags("Security")
            .WithName("GetCurrentUser");

        return endpoints;
    }

    private static async Task<IResult> CreateSession(ApplicationUser user, string authenticationMethod, QmsDbContext db, TimeProvider clock, HttpContext context, IWebHostEnvironment environment, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var activeSessions = await db.UserSessions.Where(item => item.UserId == user.Id
            && item.RevokedAtUtc == null && item.ExpiresAtUtc > now).ToListAsync(ct);
        foreach (var activeSession in activeSessions) activeSession.Revoke(now);
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = UserSession.Create(user.Id, Hash(raw), now); db.UserSessions.Add(session);
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        if (userAgent?.Length > 256) userAgent = userAgent[..256];
        db.AuditEvents.Add(AuditEvent.Create("UserSession", session.Id, 1, "UserLoggedIn", user.Id,
            user.DisplayName, now, Qms.Infrastructure.Integrity.AuditCorrelation.Current,
            JsonSerializer.SerializeToDocument(new
            {
                authenticationMethod,
                remoteAddress = context.Connection.RemoteIpAddress?.ToString(),
                userAgent
            })));
        await db.SaveChangesAsync(ct);
        var cookieName = environment.IsDevelopment()
            ? DevelopmentAuthenticationHandler.DevelopmentSessionCookieName
            : DevelopmentAuthenticationHandler.SessionCookieName;
        context.Response.Cookies.Append(cookieName, raw, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Path = "/",
            Expires = session.ExpiresAtUtc
        });
        return Results.Ok(new LoginResponse(session.ExpiresAtUtc, user.ProfileKey ?? user.Id.ToString(), user.DisplayName));
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
