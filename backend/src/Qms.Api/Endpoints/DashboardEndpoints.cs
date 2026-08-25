using Qms.Application.Dashboard;
using Qms.Application.Security;

namespace Qms.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/dashboard/summary", async (
                IDashboardService service,
                CancellationToken cancellationToken) =>
            Results.Ok(await service.GetSummaryAsync(cancellationToken)))
            .WithName("GetDashboardSummary")
            .WithTags("Dashboard")
            .RequireAuthorization(QmsPolicies.QualityView);

        return endpoints;
    }
}
