using Qms.Application.Administration;
using Qms.Application.Security;
using Qms.Contracts.Administration;

namespace Qms.Api.Endpoints;

public static class AccessAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAccessAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/access").WithTags("Access Administration").RequireAuthorization(QmsPolicies.AdministrationManage);
        admin.MapGet("/overview", async (IAccessAdministrationService service, CancellationToken ct) => Results.Ok(await service.GetOverviewAsync(ct))).WithName("GetAccessOverview");
        admin.MapPut("/users/{id:guid}", async (Guid id, UpdateUserAccessRequest request, IAccessAdministrationService service, CancellationToken ct) =>
        {
            var user = await service.UpdateUserAsync(id, request, ct); return user is null ? Results.NotFound() : Results.Ok(user);
        }).WithName("UpdateUserAccess");
        admin.MapPost("/delegations", async (CreateDelegationRequest request, IAccessAdministrationService service, CancellationToken ct) => Results.Created("/api/v1/admin/access/delegations", await service.CreateDelegationAsync(request, ct))).WithName("CreateDelegation");
        admin.MapPost("/delegations/{id:guid}/revoke", async (Guid id, IAccessAdministrationService service, CancellationToken ct) => await service.RevokeDelegationAsync(id, ct) ? Results.NoContent() : Results.NotFound()).WithName("RevokeDelegation");
        admin.MapPost("/assignments", async (CreateWorkflowAssignmentRequest request, IAccessAdministrationService service, CancellationToken ct) => Results.Created("/api/v1/admin/access/assignments", await service.CreateAssignmentAsync(request, ct))).WithName("CreateWorkflowAssignment");

        endpoints.MapGet("/api/v1/workflow/assignments/{aggregateType}/{aggregateId:guid}", async (string aggregateType, Guid aggregateId, IAccessAdministrationService service, CancellationToken ct) => Results.Ok(await service.GetAssignmentsAsync(aggregateType, aggregateId, ct)))
            .WithTags("Workflow").RequireAuthorization(QmsPolicies.QualityView).WithName("GetWorkflowAssignments");
        return endpoints;
    }
}
