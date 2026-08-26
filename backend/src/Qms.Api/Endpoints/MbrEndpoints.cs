using Microsoft.EntityFrameworkCore;
using Qms.Application.MasterBatchRecords;
using Qms.Application.Security;
using Qms.Contracts.MasterBatchRecords;

namespace Qms.Api.Endpoints;

public static class MbrEndpoints
{
    public static IEndpointRouteBuilder MapMbrEndpoints(this IEndpointRouteBuilder e)
    {
        var g = e.MapGroup("/api/v1/mbrs")
            .WithTags("Master Batch Records")
            .RequireAuthorization(QmsPolicies.MbrView);
        g.MapGet(
            "/options",
            (IMbrService s, CancellationToken ct) => Run(() => s.GetOptionsAsync(ct))
        );
        g.MapGet(
            "/lookups",
            (IMbrService s, CancellationToken ct) => Run(() => s.ListLookupsAsync(ct))
        );
        g.MapPost(
                "/lookups",
                (CreateMbrLookupRequest r, IMbrService s, CancellationToken ct) =>
                    Run(() => s.CreateLookupAsync(r, ct), true)
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPut(
                "/lookups/{id:guid}",
                (Guid id, UpdateMbrLookupRequest r, IMbrService s, CancellationToken ct) =>
                    Run(() => s.UpdateLookupAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPost(
            "/search",
            (MbrSearchRequest r, IMbrService s, CancellationToken ct) =>
                Run(() => s.SearchAsync(r, ct))
        );
        g.MapGet(
            "/{id:guid}/details",
            async (Guid id, IMbrService s, CancellationToken ct) =>
                (await s.GetDetailsAsync(id, ct)) is { } x ? Results.Ok(x) : Results.NotFound()
        );
        g.MapGet(
            "/{id:guid}/final-report",
            async (
                Guid id,
                IMbrService s,
                IMbrFinalReportService reports,
                HttpResponse response,
                CancellationToken ct
            ) =>
            {
                var d = await s.GetDetailsAsync(id, ct);
                if (d is null)
                    return Results.NotFound();
                try
                {
                    var f = await reports.EnsureGeneratedAsync(d, ct);
                    response.Headers.Append("X-Content-SHA256", f.Sha256);
                    return Results.File(f.Content, "application/pdf", f.FileName);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: 409,
                        title: "Nihai MBR çıktısı üretilemedi",
                        detail: ex.Message
                    );
                }
            }
        );
        g.MapPost(
                "/",
                (CreateMbrRequest r, IMbrService s, CancellationToken ct) =>
                    Run(() => s.CreateAsync(r, ct), true)
            )
            .RequireAuthorization(QmsPolicies.MbrCreate);
        g.MapPost(
                "/{id:guid}/steps",
                (Guid id, AddMbrStepRequest r, IMbrService s, CancellationToken ct) =>
                    Run(() => s.AddStepAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.MbrWrite);
        g.MapPost(
                "/{id:guid}/transitions",
                (Guid id, TransitionMbrRequest r, IMbrService s, CancellationToken ct) =>
                    Run(() => s.TransitionAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.MbrWrite);
        return e;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> a, bool created = false)
    {
        try
        {
            var x = await a();
            return created ? Results.Json(x, statusCode: 201)
                : x is null ? Results.NotFound()
                : Results.Ok(x);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(statusCode: 400, title: "MBR doğrulanamadı", detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: 409,
                title: "MBR iş kuralı geçişi engelledi",
                detail: ex.Message
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
