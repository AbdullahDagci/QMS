using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Application.SpecializedRecords;
using Qms.Contracts.SpecializedRecords;

namespace Qms.Api.Endpoints;

public static class SpecializedRecordEndpoints
{
    public static IEndpointRouteBuilder MapSpecializedRecordEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/v1/specialized/{module}")
            .WithTags("M.13-M.16 Specialized Records")
            .RequireAuthorization(QmsPolicies.SpecializedView);
        group.MapGet(
            "/options",
            (string module, ISpecializedRecordService service, CancellationToken ct) =>
                Run(() => service.GetOptionsAsync(Normalize(module), ct))
        );
        group.MapGet(
            "/lookups",
            (string module, ISpecializedRecordService service, CancellationToken ct) =>
                Run(() => service.ListLookupsAsync(Normalize(module), ct))
        );
        group
            .MapPost(
                "/lookups",
                (
                    string module,
                    CreateSpecializedLookupRequest request,
                    ISpecializedRecordService service,
                    CancellationToken ct
                ) => Run(() => service.CreateLookupAsync(Normalize(module), request, ct), true)
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        group
            .MapPut(
                "/lookups/{id:guid}",
                (
                    string module,
                    Guid id,
                    UpdateSpecializedLookupRequest request,
                    ISpecializedRecordService service,
                    CancellationToken ct
                ) => Run(() => service.UpdateLookupAsync(Normalize(module), id, request, ct))
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        group.MapPost(
            "/search",
            (
                string module,
                SpecializedSearchRequest request,
                ISpecializedRecordService service,
                CancellationToken ct
            ) => Run(() => service.SearchAsync(Normalize(module), request, ct))
        );
        group.MapGet(
            "/{id:guid}/details",
            async (
                string module,
                Guid id,
                ISpecializedRecordService service,
                CancellationToken ct
            ) =>
                (await service.GetDetailsAsync(Normalize(module), id, ct)) is { } x
                    ? Results.Ok(x)
                    : Results.NotFound()
        );
        group.MapGet(
            "/{id:guid}/final-report",
            async (
                string module,
                Guid id,
                ISpecializedRecordService service,
                ISpecializedFinalReportService reports,
                HttpResponse response,
                CancellationToken ct
            ) =>
            {
                var details = await service.GetDetailsAsync(Normalize(module), id, ct);
                if (details is null)
                    return Results.NotFound();
                try
                {
                    var file = await reports.EnsureGeneratedAsync(details, ct);
                    response.Headers.Append("X-Content-SHA256", file.Sha256);
                    return Results.File(file.Content, "application/pdf", file.FileName);
                }
                catch (InvalidOperationException e)
                {
                    return Results.Problem(
                        statusCode: 409,
                        title: "Nihai kayıt çıktısı üretilemedi",
                        detail: e.Message
                    );
                }
            }
        );
        group
            .MapPost(
                "/",
                (
                    string module,
                    CreateSpecializedRecordRequest request,
                    ISpecializedRecordService service,
                    CancellationToken ct
                ) => Run(() => service.CreateAsync(Normalize(module), request, ct), true)
            )
            .RequireAuthorization(QmsPolicies.SpecializedManage);
        group
            .MapPost(
                "/{id:guid}/transitions",
                (
                    string module,
                    Guid id,
                    TransitionSpecializedRecordRequest request,
                    ISpecializedRecordService service,
                    CancellationToken ct
                ) => Run(() => service.TransitionAsync(Normalize(module), id, request, ct))
            )
            .RequireAuthorization(QmsPolicies.SpecializedManage);
        return endpoints;
    }

    private static string Normalize(string module) =>
        module.ToLowerInvariant() switch
        {
            "m13" => "M.13",
            "m14" => "M.14",
            "m15" => "M.15",
            "m16" => "M.16",
            _ => throw new ArgumentException("Modül kodu geçersizdir."),
        };

    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false)
    {
        try
        {
            var x = await action();
            return created ? Results.Json(x, statusCode: 201)
                : x is null ? Results.NotFound()
                : Results.Ok(x);
        }
        catch (ArgumentException e)
        {
            return Results.Problem(
                statusCode: 400,
                title: "Kayıt doğrulanamadı",
                detail: e.Message
            );
        }
        catch (InvalidOperationException e)
        {
            return Results.Problem(
                statusCode: 409,
                title: "İş kuralı geçişi engelledi",
                detail: e.Message
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Eşzamanlı güncelleme çakışması",
                detail: "Kayıt değişti; ekranı yenileyin."
            );
        }
    }
}
