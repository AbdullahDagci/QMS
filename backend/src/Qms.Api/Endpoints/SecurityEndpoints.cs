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

namespace Qms.Api.Endpoints;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/auth/login", async (LoginRequest request, UserManager<ApplicationUser> userManager, QmsDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password)) return Results.Unauthorized();
            return await CreateSession(user, db, clock, ct);
        }).AllowAnonymous().WithTags("Security");

        endpoints.MapPost("/api/v1/auth/quick-login", async (QuickLoginRequest request, IWebHostEnvironment environment, QmsDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            if (!environment.IsDevelopment()) return Results.NotFound();
            var user = await db.Users.SingleOrDefaultAsync(x => x.ProfileKey == request.ProfileKey && x.IsActive, ct);
            return user is null ? Results.NotFound() : await CreateSession(user, db, clock, ct);
        }).AllowAnonymous().WithTags("Security");
        endpoints.MapGet("/api/v1/auth/quick-profiles", (IWebHostEnvironment environment) => environment.IsDevelopment()
            ? Results.Ok(DevelopmentProfiles.All.Select(x => new DevelopmentProfileResponse(x.Key, x.DisplayName, x.DepartmentName, x.Roles)))
            : Results.NotFound()).AllowAnonymous().WithTags("Security");

        endpoints.MapPost("/api/v1/auth/logout", async (HttpRequest request, QmsDbContext db, TimeProvider clock, CancellationToken ct) =>
        {
            var raw = request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(raw)) { var hash = Hash(raw); var session = await db.UserSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, ct); if (session is not null) { session.Revoke(clock.GetUtcNow()); await db.SaveChangesAsync(ct); } }
            return Results.NoContent();
        }).RequireAuthorization().WithTags("Security");

        endpoints.MapGet("/api/v1/auth/me", async (
                ClaimsPrincipal user,
                IAuthorizationService authorizationService) =>
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
                    DevelopmentProfiles.All.Select(profile => new DevelopmentProfileResponse(
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

    private static async Task<IResult> CreateSession(ApplicationUser user, QmsDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = UserSession.Create(user.Id, Hash(raw), clock.GetUtcNow()); db.UserSessions.Add(session); await db.SaveChangesAsync(ct);
        return Results.Ok(new LoginResponse(raw, session.ExpiresAtUtc, user.ProfileKey ?? user.Id.ToString(), user.DisplayName));
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
