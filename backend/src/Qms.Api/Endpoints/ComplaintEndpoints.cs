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
        group.MapGet("/options", async (IComplaintService service, CancellationToken ct) => Results.Ok(await service.GetOptionsAsync(ct)));
        group.MapGet("/lookup-definitions", async (IComplaintService service, CancellationToken ct) => Results.Ok(await service.ListLookupDefinitionsAsync(ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapPost("/lookup-definitions", (CreateComplaintLookupDefinitionRequest request, IComplaintService service, CancellationToken ct) => Execute(() => service.CreateLookupDefinitionAsync(request, ct), true)).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapPut("/lookup-definitions/{id:guid}", (Guid id, UpdateComplaintLookupDefinitionRequest request, IComplaintService service, CancellationToken ct) => Execute(() => service.UpdateLookupDefinitionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapGet("/{id:guid}/details", async (Guid id, IComplaintService service, CancellationToken ct) => (await service.GetDetailsAsync(id, ct)) is { } result ? Results.Ok(result) : Results.NotFound());
        group.MapGet("/{id:guid}/final-report", async (Guid id, IComplaintService service, IComplaintFinalReportService reports, HttpResponse response, CancellationToken ct) => { var details = await service.GetDetailsAsync(id, ct); if (details is null) return Results.NotFound(); try { var file = await reports.EnsureGeneratedAsync(details, ct); response.Headers.Append("X-Content-SHA256", file.Sha256); return Results.File(file.Content, "application/pdf", file.FileName); } catch (InvalidOperationException e) { return Results.Problem(statusCode: 409, title: "Nihai şikâyet çıktısı üretilemedi", detail: e.Message); } }).WithName("DownloadComplaintFinalReport");
        group.MapPost("/", (CreateComplaintRequest request, IComplaintService service, CancellationToken ct) => Execute(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.ComplaintCreate);
        group.MapPost("/{id:guid}/transitions", (Guid id, TransitionComplaintRequest request, IComplaintService service, CancellationToken ct) => Mutate(() => service.TransitionAsync(id, request, ct)));
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

