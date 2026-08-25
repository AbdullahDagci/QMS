using Microsoft.EntityFrameworkCore;
using Qms.Application.InternalAudits;
using Qms.Application.Security;
using Qms.Contracts.InternalAudits;

namespace Qms.Api.Endpoints;

public static class InternalAuditEndpoints
{
    public static IEndpointRouteBuilder MapInternalAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/internal-audits").WithTags("Internal Audits").RequireAuthorization(QmsPolicies.InternalAuditView);
        group.MapPost("/search", (InternalAuditSearchRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.SearchAsync(request, ct)));
        group.MapGet("/{id:guid}/details", async (Guid id, IInternalAuditService service, CancellationToken ct) => (await service.GetDetailsAsync(id, ct)) is { } value ? Results.Ok(value) : Results.NotFound());
        group.MapPost("/", (CreateInternalAuditRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.InternalAuditPlan);
        group.MapPost("/{id:guid}/transitions", (Guid id, TransitionInternalAuditRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.TransitionAsync(id, request, ct)));
        group.MapPost("/{id:guid}/checklist/{itemId:guid}/answer", (Guid id, Guid itemId, AnswerAuditQuestionRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.AnswerAsync(id, itemId, request, ct))).RequireAuthorization(QmsPolicies.InternalAuditExecute);
        group.MapPost("/{id:guid}/findings", (Guid id, AddAuditFindingRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.AddFindingAsync(id, request, ct))).RequireAuthorization(QmsPolicies.InternalAuditExecute);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/response", (Guid id, Guid findingId, RespondAuditFindingRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.RespondFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.InternalAuditRespond);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/close", (Guid id, Guid findingId, CloseAuditFindingRequest request, IInternalAuditService service, CancellationToken ct) => Run(() => service.CloseFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.InternalAuditApprove);
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false)
    {
        try { var result = await action(); return created ? Results.Json(result, statusCode: 201) : result is null ? Results.NotFound() : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "İç denetim doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti; ekranı yenileyin."); }
    }
}
