using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Qms.Application.Security;
using Qms.Contracts.Security;
using Qms.Infrastructure.Identity;

namespace Qms.Api.Endpoints;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
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
                        profile.Roles)).ToList()));
            })
            .RequireAuthorization()
            .WithTags("Security")
            .WithName("GetCurrentUser");

        return endpoints;
    }
}
