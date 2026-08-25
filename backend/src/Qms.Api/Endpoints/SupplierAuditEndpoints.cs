using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Application.SupplierAudits;
using Qms.Contracts.SupplierAudits;

namespace Qms.Api.Endpoints;

public static class SupplierAuditEndpoints
{
    public static IEndpointRouteBuilder MapSupplierAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/supplier-audits").WithTags("Supplier Audits").RequireAuthorization(QmsPolicies.SupplierAuditView);
        group.MapPost("/search", (SupplierAuditSearchRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.SearchAsync(request, ct)));
        group.MapGet("/{id:guid}/details", async (Guid id, ISupplierAuditService service, CancellationToken ct) => (await service.GetDetailsAsync(id, ct)) is { } value ? Results.Ok(value) : Results.NotFound());
        group.MapPost("/", (CreateSupplierAuditRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.SupplierAuditPlan);
        group.MapPost("/{id:guid}/transitions", (Guid id, TransitionSupplierAuditRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.TransitionAsync(id, request, ct)));
        group.MapPost("/{id:guid}/checklist/{itemId:guid}/answer", (Guid id, Guid itemId, AnswerSupplierAuditChecklistRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.AnswerChecklistAsync(id, itemId, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditExecute);
        group.MapPost("/{id:guid}/findings", (Guid id, AddSupplierAuditFindingRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.AddFindingAsync(id, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditExecute);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/response", (Guid id, Guid findingId, RespondSupplierAuditFindingRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.RespondFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditRespond);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/evidence", (Guid id, Guid findingId, SubmitSupplierAuditEvidenceRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.SubmitEvidenceAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditRespond);
        group.MapPost("/{id:guid}/findings/{findingId:guid}/close", (Guid id, Guid findingId, CloseSupplierAuditFindingRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.CloseFindingAsync(id, findingId, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditApprove);
        group.MapPost("/{id:guid}/invitations", (Guid id, CreateSupplierInvitationRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.CreateInvitationAsync(id, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditRespond);
        group.MapPost("/{id:guid}/result", (Guid id, RecordSupplierAuditResultRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.RecordResultAsync(id, request, ct))).RequireAuthorization(QmsPolicies.SupplierAuditApprove);
        endpoints.MapPost("/api/v1/supplier-audit-invitations/respond", (SupplierInvitationSubmissionRequest request, ISupplierAuditService service, CancellationToken ct) => Run(() => service.SubmitInvitationResponseAsync(request, ct))).AllowAnonymous().WithTags("Supplier Audit Portal");
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false)
    {
        try { var result = await action(); return created ? Results.Json(result, statusCode: 201) : result is null ? Results.NotFound() : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Tedarikçi denetimi doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti; ekranı yenileyin."); }
    }
}
