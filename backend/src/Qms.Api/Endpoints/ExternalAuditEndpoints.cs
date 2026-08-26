using Microsoft.EntityFrameworkCore;
using Qms.Application.ExternalAudits;
using Qms.Application.Security;
using Qms.Contracts.ExternalAudits;

namespace Qms.Api.Endpoints;

public static class ExternalAuditEndpoints
{
    public static IEndpointRouteBuilder MapExternalAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/external-audits").WithTags("External Audits").RequireAuthorization(QmsPolicies.ExternalAuditView);
        group.MapPost("/search", (ExternalAuditSearchRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.SearchAsync(request, ct)));
        group.MapGet("/options", (IExternalAuditService service, CancellationToken ct) => Run(() => service.GetOptionsAsync(ct)));
        group.MapGet("/lookups", (IExternalAuditService service, CancellationToken ct) => Run(() => service.ListLookupDefinitionsAsync(ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapPost("/lookups", (CreateExternalAuditLookupDefinitionRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.CreateLookupDefinitionAsync(request, ct), true)).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapPut("/lookups/{lookupId:guid}", (Guid lookupId, UpdateExternalAuditLookupDefinitionRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.UpdateLookupDefinitionAsync(lookupId, request, ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapGet("/{id:guid}/details", async (Guid id, IExternalAuditService service, CancellationToken ct) => (await service.GetDetailsAsync(id, ct)) is { } value ? Results.Ok(value) : Results.NotFound());
        group.MapGet("/{id:guid}/final-report",async(Guid id,IExternalAuditService service,IExternalAuditFinalReportService reports,HttpResponse response,CancellationToken ct)=>{var details=await service.GetDetailsAsync(id,ct);if(details is null)return Results.NotFound();try{var file=await reports.EnsureGeneratedAsync(details,ct);response.Headers.Append("X-Content-SHA256",file.Sha256);return Results.File(file.Content,"application/pdf",file.FileName);}catch(InvalidOperationException e){return Results.Problem(statusCode:409,title:"Nihai dış denetim çıktısı üretilemedi",detail:e.Message);}});
        group.MapPost("/", (CreateExternalAuditRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.ExternalAuditCreate);
        group.MapPost("/{id:guid}/transitions", (Guid id, TransitionExternalAuditRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.TransitionAsync(id, request, ct)));
        group.MapPost("/{id:guid}/documents/{requestId:guid}/export", (Guid id, Guid requestId, ExportExternalAuditDocumentRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.ExportDocumentAsync(id, requestId, request, ct))).RequireAuthorization(QmsPolicies.ExternalAuditPrepare);
        group.MapPost("/{id:guid}/findings", (Guid id, AddExternalAuditFindingRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.AddFindingAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ExternalAuditPrepare);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/response", (Guid id, Guid findingId, RespondExternalAuditFindingRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.RespondFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.ExternalAuditRespond);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/close", (Guid id, Guid findingId, CloseExternalAuditFindingRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.CloseFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.ExternalAuditApprove);
        group.MapPost("/{id:guid}/closure-letter", (Guid id, RecordExternalAuditClosureLetterRequest request, IExternalAuditService service, CancellationToken ct) => Run(() => service.RecordClosureLetterAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ExternalAuditApprove);
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false)
    {
        try { var result = await action(); return created ? Results.Json(result, statusCode: 201) : result is null ? Results.NotFound() : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Dış denetim doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti; ekranı yenileyin."); }
    }
}
