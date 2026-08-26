using Microsoft.EntityFrameworkCore;
using Qms.Application.Documents;
using Qms.Application.Security;
using Qms.Contracts.Documents;
namespace Qms.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g = endpoints.MapGroup("/api/v1/documents").WithTags("Documents").RequireAuthorization(QmsPolicies.QualityView);
        g.MapGet("/lookups", async (IDocumentService s, CancellationToken ct) => Results.Ok(await s.GetLookupsAsync(ct)));
        g.MapGet("/lookup-definitions", async (IDocumentService s, CancellationToken ct) => Results.Ok(await s.ListLookupDefinitionsAsync(ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPost("/lookup-definitions", async (CreateDocumentLookupDefinitionRequest r, IDocumentService s, CancellationToken ct) => await Run(() => s.CreateLookupDefinitionAsync(r, ct), true)).RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPut("/lookup-definitions/{id:guid}", async (Guid id, UpdateDocumentLookupDefinitionRequest r, IDocumentService s, CancellationToken ct) => await Run(() => s.UpdateLookupDefinitionAsync(id, r, ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPost("/search", async (ControlledDocumentSearchRequest r, IDocumentService s, CancellationToken ct) => await Run(() => s.SearchAsync(r, ct)));
        g.MapGet("/{id:guid}/details", async (Guid id, IDocumentService s, CancellationToken ct) => await s.GetDetailsAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound());
        g.MapGet("/{id:guid}/final-report", async (Guid id, IDocumentService s, IDocumentFinalReportService reports, HttpResponse response, CancellationToken ct) =>
        {
            var details = await s.GetDetailsAsync(id, ct); if (details is null) return Results.NotFound();
            try { var file = await reports.EnsureGeneratedAsync(details, ct); response.Headers.Append("X-Content-SHA256", file.Sha256); return Results.File(file.Content, "application/pdf", file.FileName); }
            catch (InvalidOperationException e) { return Results.Problem(statusCode: 409, title: "Nihai doküman çıktısı üretilemedi", detail: e.Message); }
        }).WithName("DownloadDocumentFinalReport");
        g.MapPost("/", async (CreateControlledDocumentRequest r, IDocumentService s, CancellationToken ct) => await Run(() => s.CreateAsync(r, ct), true)).RequireAuthorization(QmsPolicies.DocumentCreate);
        g.MapPut("/{id:guid}/draft", async (Guid id, UpdateDocumentDraftRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.UpdateDraftAsync(id, r, ct))).RequireAuthorization(QmsPolicies.DocumentWrite);
        g.MapPost("/{id:guid}/reviews/{reviewId:guid}/complete", async (Guid id, Guid reviewId, CompleteDocumentReviewRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.CompleteReviewAsync(id, reviewId, r, ct))).RequireAuthorization(QmsPolicies.DocumentReview);
        g.MapPost("/{id:guid}/training/{requirementId:guid}/complete", async (Guid id, Guid requirementId, CompleteDocumentTrainingRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.CompleteTrainingAsync(id, requirementId, r, ct))).RequireAuthorization(QmsPolicies.DocumentApprove);
        g.MapPost("/{id:guid}/revisions", async (Guid id, StartDocumentRevisionRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.StartRevisionAsync(id, r, ct))).RequireAuthorization(QmsPolicies.DocumentWrite);
        g.MapPost("/{id:guid}/copies", async (Guid id, IssueControlledCopyRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.IssueCopyAsync(id, r, ct))).RequireAuthorization(QmsPolicies.DocumentDistribute);
        g.MapPost("/{id:guid}/copies/{copyId:guid}/close", async (Guid id, Guid copyId, CloseControlledCopyRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.CloseCopyAsync(id, copyId, r, ct))).RequireAuthorization(QmsPolicies.DocumentDistribute);
        g.MapPost("/{id:guid}/read-receipts", async (Guid id, AcknowledgeDocumentReadRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.AcknowledgeReadAsync(id, r, ct))).RequireAuthorization(QmsPolicies.DocumentRead);
        g.MapPost("/{id:guid}/transitions", async (Guid id, TransitionDocumentRequest r, IDocumentService s, CancellationToken ct) => await Mutate(() => s.TransitionAsync(id, r, ct))).RequireAuthorization(QmsPolicies.DocumentApprove);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false) { try { var value = await action(); return created ? Results.Json(value, statusCode: 201) : Results.Ok(value); } catch (ArgumentException e) { return Results.Problem(statusCode: 400, title: "Doküman doğrulanamadı", detail: e.Message); } catch (InvalidOperationException e) { return Results.Problem(statusCode: 409, title: "Doküman iş kuralı geçişi engelledi", detail: e.Message); } catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti. Ekranı yenileyip tekrar deneyin."); } }
    private static Task<IResult> Mutate(Func<Task<ControlledDocumentDetailsResponse?>> action) => Run(action);
}
