using Microsoft.EntityFrameworkCore;
using Qms.Application.Deviations;
using Qms.Contracts.Deviations;
using Qms.Application.Security;

namespace Qms.Api.Endpoints;

public static class DeviationEndpoints
{
    public static IEndpointRouteBuilder MapDeviationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/deviations")
            .WithTags("Deviations")
            .RequireAuthorization(QmsPolicies.QualityView);

        group.MapGet("/", async (IDeviationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .WithName("ListDeviations");

        group.MapGet("/lookups", async (IDeviationService service, CancellationToken ct) =>
                Results.Ok(await service.GetLookupsAsync(ct)))
            .WithName("GetDeviationLookups");

        group.MapGet("/types", async (IDeviationService service, CancellationToken ct) =>
                Results.Ok(await service.ListDeviationTypesAsync(ct)))
            .RequireAuthorization(QmsPolicies.AdministrationManage)
            .WithName("ListDeviationTypes");

        group.MapPost("/types", async (CreateDeviationTypeRequest request, IDeviationService service, CancellationToken ct) =>
            {
                try { return Results.Created("/api/v1/deviations/types", await service.CreateDeviationTypeAsync(request, ct)); }
                catch (ArgumentException exception) { return ValidationProblem(exception); }
            })
            .RequireAuthorization(QmsPolicies.AdministrationManage)
            .WithName("CreateDeviationType");

        group.MapPut("/types/{id:guid}", async (Guid id, UpdateDeviationTypeRequest request, IDeviationService service, CancellationToken ct) =>
            {
                try
                {
                    var result = await service.UpdateDeviationTypeAsync(id, request, ct);
                    return result is null ? Results.NotFound() : Results.Ok(result);
                }
                catch (ArgumentException exception) { return ValidationProblem(exception); }
            })
            .RequireAuthorization(QmsPolicies.AdministrationManage)
            .WithName("UpdateDeviationType");

        group.MapGet("/assignment-rules", async (IDeviationService service, CancellationToken ct) => Results.Ok(await service.ListAssignmentRulesAsync(ct)))
            .RequireAuthorization(QmsPolicies.AdministrationManage).WithName("ListDeviationAssignmentRules");
        group.MapPost("/assignment-rules", async (SaveDeviationAssignmentRuleRequest request, IDeviationService service, CancellationToken ct) =>
            {
                try { return Results.Created("/api/v1/deviations/assignment-rules", await service.CreateAssignmentRuleAsync(request, ct)); }
                catch (ArgumentException exception) { return ValidationProblem(exception); }
            }).RequireAuthorization(QmsPolicies.AdministrationManage).WithName("CreateDeviationAssignmentRule");
        group.MapPut("/assignment-rules/{id:guid}", async (Guid id, SaveDeviationAssignmentRuleRequest request, IDeviationService service, CancellationToken ct) =>
            {
                try { var result = await service.UpdateAssignmentRuleAsync(id, request, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
                catch (ArgumentException exception) { return ValidationProblem(exception); }
            }).RequireAuthorization(QmsPolicies.AdministrationManage).WithName("UpdateDeviationAssignmentRule");

        group.MapPost("/search", async (
                DeviationSearchRequest request,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.SearchAsync(request, cancellationToken));
                }
                catch (ArgumentException exception)
                {
                    return ValidationProblem(exception);
                }
            })
            .WithName("SearchDeviations");

        group.MapGet("/{id:guid}", async (
                Guid id,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            {
                var deviation = await service.GetAsync(id, cancellationToken);
                return deviation is null ? Results.NotFound() : Results.Ok(deviation);
            })
            .WithName("GetDeviation");

        group.MapGet("/{id:guid}/details", async (
                Guid id,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            {
                var deviation = await service.GetDetailsAsync(id, cancellationToken);
                return deviation is null ? Results.NotFound() : Results.Ok(deviation);
            })
            .WithName("GetDeviationDetails");

        group.MapGet("/{id:guid}/final-report", async (
                Guid id,
                IDeviationService service,
                IDeviationFinalReportService reportService,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var details = await service.GetDetailsAsync(id, cancellationToken);
                    if (details is null) return Results.NotFound();
                    var report = await reportService.EnsureGeneratedAsync(details, cancellationToken);
                    httpContext.Response.Headers["X-Document-SHA256"] = report.Sha256;
                    httpContext.Response.Headers["Cache-Control"] = "private, no-store";
                    return Results.File(report.Content, "application/pdf", report.FileName, enableRangeProcessing: true);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Nihai rapor kullanılamıyor", detail: exception.Message);
                }
            })
            .WithName("DownloadDeviationFinalReport");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(QmsPolicies.DeviationCreate)
            .WithName("CreateDeviation");

        group.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization(QmsPolicies.DeviationCreate)
            .WithName("SubmitDeviation");

        group.MapPost("/{id:guid}/investigations", async (
                Guid id,
                AddDeviationInvestigationRequest request,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            await ExecuteDetailsMutationAsync(
                () => service.AddInvestigationAsync(id, request, cancellationToken)))
            .RequireAuthorization(QmsPolicies.DeviationInvestigate)
            .WithName("AddDeviationInvestigation");

        group.MapPost("/{id:guid}/batch-impacts", async (
                Guid id,
                AddDeviationBatchImpactRequest request,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            await ExecuteDetailsMutationAsync(
                () => service.AddBatchImpactAsync(id, request, cancellationToken)))
            .RequireAuthorization(QmsPolicies.DeviationInvestigate)
            .WithName("AddDeviationBatchImpact");

        group.MapPost("/{id:guid}/transitions", async (
                Guid id,
                TransitionDeviationRequest request,
                IDeviationService service,
                CancellationToken cancellationToken) =>
            await ExecuteDetailsMutationAsync(
                () => service.TransitionAsync(id, request, cancellationToken)))
            .RequireAuthorization(QmsPolicies.DeviationManage)
            .WithName("TransitionDeviation");

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateDeviationRequest request,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var deviation = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/v1/deviations/{deviation.Id}", deviation);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception);
        }
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        SubmitDeviationRequest request,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var deviation = await service.SubmitAsync(id, request, cancellationToken);
            return deviation is null ? Results.NotFound() : Results.Ok(deviation);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Durum geçişi uygulanamadı",
                detail: exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Eşzamanlı güncelleme çakışması",
                detail: "Kayıt siz işlem yaparken değişti. Ekranı yenileyip tekrar deneyin.");
        }
    }

    private static IResult ValidationProblem(ArgumentException exception) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Sapma kaydı doğrulanamadı",
            detail: exception.Message);

    private static async Task<IResult> ExecuteDetailsMutationAsync(
        Func<Task<DeviationDetailsResponse?>> action)
    {
        try
        {
            var result = await action();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "İş kuralı geçişi engelledi",
                detail: exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Eşzamanlı güncelleme çakışması",
                detail: "Kayıt siz işlem yaparken değişti. Ekranı yenileyip tekrar deneyin.");
        }
    }
}
