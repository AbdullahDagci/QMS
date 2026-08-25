using Microsoft.EntityFrameworkCore;
using Qms.Application.Complaints;
using Qms.Application.Security;
using Qms.Contracts.Complaints;

namespace Qms.Api.Endpoints;

public static class ComplaintEndpoints
{
    public static IEndpointRouteBuilder MapComplaintEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/complaints").WithTags("Complaints").RequireAuthorization(QmsPolicies.ComplaintView);
        group.MapPost("/search", (ComplaintSearchRequest request, IComplaintService service, CancellationToken ct) => Execute(() => service.SearchAsync(request, ct)));
        group.MapGet("/{id:guid}/details", async (Guid id, IComplaintService service, CancellationToken ct) => (await service.GetDetailsAsync(id, ct)) is { } result ? Results.Ok(result) : Results.NotFound());
        group.MapPost("/", (CreateComplaintRequest request, IComplaintService service, CancellationToken ct) => Execute(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.ComplaintCreate);
        group.MapPost("/{id:guid}/transitions", (Guid id, TransitionComplaintRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.TransitionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ComplaintManage);
        group.MapPost("/{id:guid}/responses", (Guid id, AddComplaintResponseRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.AddResponseAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ComplaintManage);
        group.MapPost("/{id:guid}/responses/{responseId:guid}/approve", (Guid id, Guid responseId, ApproveComplaintResponseRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.ApproveResponseAsync(id, responseId, request, ct))).RequireAuthorization(QmsPolicies.ComplaintApprove);
        group.MapPost("/{id:guid}/investigations/{investigationId:guid}/complete", (Guid id, Guid investigationId, CompleteComplaintInvestigationRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.CompleteInvestigationAsync(id, investigationId, request, ct))).RequireAuthorization(QmsPolicies.ComplaintInvestigate);
        group.MapPost("/{id:guid}/impact", (Guid id, CompleteComplaintImpactRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.CompleteImpactAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ComplaintManage);
        group.MapPost("/{id:guid}/capa-decision", (Guid id, DecideComplaintCapaRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.DecideCapaAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ComplaintManage);
        return endpoints;
    }
    private static async Task<IResult> Execute<T>(Func<Task<T>> action, bool created=false){try{var result=await action();return created?Results.Json(result,statusCode:201):Results.Ok(result);}catch(ArgumentException ex){return Results.Problem(statusCode:400,title:"Şikâyet doğrulanamadı",detail:ex.Message);}catch(InvalidOperationException ex){return Results.Problem(statusCode:409,title:"İş kuralı geçişi engelledi",detail:ex.Message);}catch(DbUpdateConcurrencyException){return Results.Problem(statusCode:409,title:"Eşzamanlı güncelleme çakışması",detail:"Kayıt değişti. Ekranı yenileyip tekrar deneyin.");}}
    private static async Task<IResult> Mutate(Func<Task<ComplaintDetailsResponse?>> action){var result=await Execute(action);return result;}
}

