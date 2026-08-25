using Microsoft.EntityFrameworkCore;
using Qms.Application.Capas;
using Qms.Contracts.Capas;
using Qms.Application.Security;

namespace Qms.Api.Endpoints;

public static class CapaEndpoints
{
    public static IEndpointRouteBuilder MapCapaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/capas").WithTags("CAPA").RequireAuthorization(QmsPolicies.QualityView);
        group.MapPost("/search", async (CapaSearchRequest request, ICapaService service, CancellationToken ct) => await Execute(() => service.SearchAsync(request, ct)));
        group.MapGet("/{id:guid}/details", async (Guid id, ICapaService service, CancellationToken ct) => { var result = await service.GetDetailsAsync(id, ct); return result is null ? Results.NotFound() : Results.Ok(result); });
        group.MapPost("/", async (CreateCapaRequest request, ICapaService service, CancellationToken ct) => await Execute(async () => { var result = await service.CreateAsync(request, ct); return result; }, true)).RequireAuthorization(QmsPolicies.CapaPlan);
        group.MapPost("/{id:guid}/actions", async (Guid id, AddCapaActionRequest request, ICapaService service, CancellationToken ct) => await Mutate(() => service.AddActionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.CapaPlan);
        group.MapPost("/{id:guid}/actions/{actionId:guid}/complete", async (Guid id, Guid actionId, CompleteCapaActionRequest request, ICapaService service, CancellationToken ct) => await Mutate(() => service.CompleteActionAsync(id, actionId, request, ct))).RequireAuthorization(QmsPolicies.CapaCompleteAction);
        group.MapPost("/{id:guid}/actions/{actionId:guid}/verify", async (Guid id, Guid actionId, VerifyCapaActionRequest request, ICapaService service, CancellationToken ct) => await Mutate(() => service.VerifyActionAsync(id, actionId, request, ct))).RequireAuthorization(QmsPolicies.CapaVerify);
        group.MapPost("/{id:guid}/transitions", async (Guid id, TransitionCapaRequest request, ICapaService service, CancellationToken ct) => await Mutate(() => service.TransitionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.CapaManage);
        endpoints.MapPost("/api/v1/deviations/{deviationId:guid}/capa", async (Guid deviationId, CreateCapaRequest request, ICapaService service, CancellationToken ct) => await Execute(() => service.CreateAsync(request with { SourceDeviationId = deviationId, SourceType = "Deviation" }, ct), true)).WithTags("Deviations", "CAPA").RequireAuthorization(QmsPolicies.CapaPlan);
        return endpoints;
    }

    private static async Task<IResult> Execute<T>(Func<Task<T>> action, bool created = false)
    {
        try { var result = await action(); return created ? Results.Json(result, statusCode: StatusCodes.Status201Created) : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "DÖF kaydı doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti. Ekranı yenileyip tekrar deneyin."); }
    }
    private static async Task<IResult> Mutate(Func<Task<CapaDetailsResponse?>> action)
    {
        try { var result = await action(); return result is null ? Results.NotFound() : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "DÖF kaydı doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti. Ekranı yenileyip tekrar deneyin."); }
    }
}
