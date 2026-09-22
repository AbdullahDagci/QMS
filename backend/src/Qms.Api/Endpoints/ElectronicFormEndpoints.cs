using Qms.Application.ElectronicForms;
using Qms.Application.Security;
using Qms.Contracts.ElectronicForms;

namespace Qms.Api.Endpoints;

public static class ElectronicFormEndpoints
{
    public static IEndpointRouteBuilder MapElectronicFormEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/electronic-forms")
            .WithTags("Electronic Forms")
            .RequireAuthorization(QmsPolicies.FormView);

        group.MapGet("/definitions", async (IElectronicFormService service, CancellationToken ct) =>
            Results.Ok(await service.ListDefinitionsAsync(ct))).WithName("ListElectronicForms");
        group.MapGet("/definitions/{id:guid}", async (Guid id, IElectronicFormService service, CancellationToken ct) =>
            await service.GetDefinitionAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .WithName("GetElectronicForm");
        group.MapPost("/definitions", async (CreateElectronicFormRequest request, IElectronicFormService service, CancellationToken ct) =>
        {
            var result = await service.CreateDefinitionAsync(request, ct);
            return Results.Created($"/api/v1/electronic-forms/definitions/{result.Definition.Id}", result);
        }).RequireAuthorization(QmsPolicies.FormManage).WithName("CreateElectronicForm");
        group.MapPut("/definitions/{id:guid}/draft", async (Guid id, UpdateElectronicFormDraftRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.UpdateDraftAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormManage).WithName("UpdateElectronicFormDraft");
        group.MapPost("/definitions/{id:guid}/submit-review", async (Guid id, TransitionElectronicFormVersionRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.SubmitForReviewAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormManage).WithName("SubmitElectronicFormForReview");
        group.MapPost("/definitions/{id:guid}/publish", async (Guid id, TransitionElectronicFormVersionRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.PublishAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormApprove).WithName("PublishElectronicForm");
        group.MapPost("/definitions/{id:guid}/versions", async (Guid id, StartElectronicFormVersionRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.StartNewVersionAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormManage).WithName("StartElectronicFormVersion");

        group.MapGet("/records", async (IElectronicFormService service, CancellationToken ct) =>
            Results.Ok(await service.ListRecordsAsync(ct))).WithName("ListElectronicFormRecords");
        group.MapGet("/records/{id:guid}", async (Guid id, IElectronicFormService service, CancellationToken ct) =>
            await service.GetRecordAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .WithName("GetElectronicFormRecord");
        group.MapPost("/records", async (CreateElectronicFormRecordRequest request, IElectronicFormService service, CancellationToken ct) =>
        {
            var result = await service.CreateRecordAsync(request, ct);
            return Results.Created($"/api/v1/electronic-forms/records/{result.Record.Id}", result);
        }).RequireAuthorization(QmsPolicies.FormUse).WithName("CreateElectronicFormRecord");
        group.MapPut("/records/{id:guid}/draft", async (Guid id, UpdateElectronicFormRecordRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.UpdateRecordDraftAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormUse).WithName("UpdateElectronicFormRecord");
        group.MapPost("/records/{id:guid}/submit", async (Guid id, SubmitElectronicFormRecordRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.SubmitRecordAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormUse).WithName("SubmitElectronicFormRecord");
        group.MapPost("/records/{id:guid}/approve", async (Guid id, ApproveElectronicFormRecordRequest request,
            IElectronicFormService service, CancellationToken ct) =>
            await service.ApproveRecordAsync(id, request, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequireAuthorization(QmsPolicies.FormApprove).WithName("ApproveElectronicFormRecord");
        group.MapGet("/records/{id:guid}/final-report", async (Guid id, IElectronicFormService service,
            IElectronicFormFinalReportService reports, HttpResponse response, CancellationToken ct) =>
        {
            var details = await service.GetRecordAsync(id, ct);
            if (details is null) return Results.NotFound();
            var file = await reports.EnsureGeneratedAsync(details, ct);
            response.Headers.Append("X-Content-SHA256", file.Sha256);
            response.Headers.Append("Cache-Control", "private, immutable");
            return Results.File(file.Content, "application/pdf", file.FileName);
        }).WithName("DownloadElectronicFormFinalReport");
        return endpoints;
    }
}
