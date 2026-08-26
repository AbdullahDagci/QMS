using Microsoft.EntityFrameworkCore;
using Qms.Application.RiskManagement;
using Qms.Application.Security;
using Qms.Contracts.RiskManagement;

namespace Qms.Api.Endpoints;

public static class RiskManagementEndpoints
{
    public static IEndpointRouteBuilder MapRiskManagementEndpoints(this IEndpointRouteBuilder e)
    {
        var g = e.MapGroup("/api/v1/risks")
            .WithTags("Risk Management")
            .RequireAuthorization(QmsPolicies.RiskView);
        g.MapGet(
            "/options",
            (IRiskManagementService s, CancellationToken ct) => Run(() => s.GetOptionsAsync(ct))
        );
        g.MapGet(
            "/lookups",
            (IRiskManagementService s, CancellationToken ct) => Run(() => s.ListLookupsAsync(ct))
        );
        g.MapPost(
                "/lookups",
                (CreateRiskLookupRequest r, IRiskManagementService s, CancellationToken ct) =>
                    Run(() => s.CreateLookupAsync(r, ct), true)
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPut(
                "/lookups/{id:guid}",
                (
                    Guid id,
                    UpdateRiskLookupRequest r,
                    IRiskManagementService s,
                    CancellationToken ct
                ) => Run(() => s.UpdateLookupAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPost(
            "/search",
            (RiskSearchRequest r, IRiskManagementService s, CancellationToken ct) =>
                Run(() => s.SearchAsync(r, ct))
        );
        g.MapGet(
            "/{id:guid}/details",
            async (Guid id, IRiskManagementService s, CancellationToken ct) =>
                (await s.GetDetailsAsync(id, ct)) is { } x ? Results.Ok(x) : Results.NotFound()
        );
        g.MapPost(
                "/",
                (CreateRiskAssessmentRequest r, IRiskManagementService s, CancellationToken ct) =>
                    Run(() => s.CreateAsync(r, ct), true)
            )
            .RequireAuthorization(QmsPolicies.RiskCreate);
        g.MapPost(
                "/{id:guid}/items",
                (Guid id, AddRiskItemRequest r, IRiskManagementService s, CancellationToken ct) =>
                    Run(() => s.AddItemAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.RiskManage);
        g.MapPost(
                "/{id:guid}/items/{itemId:guid}/complete",
                (
                    Guid id,
                    Guid itemId,
                    CompleteRiskActionRequest r,
                    IRiskManagementService s,
                    CancellationToken ct
                ) => Run(() => s.CompleteActionAsync(id, itemId, r, ct))
            )
            .RequireAuthorization(QmsPolicies.RiskManage);
        g.MapPost(
                "/{id:guid}/items/{itemId:guid}/residual",
                (
                    Guid id,
                    Guid itemId,
                    SetResidualRiskRequest r,
                    IRiskManagementService s,
                    CancellationToken ct
                ) => Run(() => s.SetResidualAsync(id, itemId, r, ct))
            )
            .RequireAuthorization(QmsPolicies.RiskManage);
        g.MapPost(
                "/{id:guid}/transitions",
                (
                    Guid id,
                    TransitionRiskRequest r,
                    IRiskManagementService s,
                    CancellationToken ct
                ) => Run(() => s.TransitionAsync(id, r, ct))
            )
            .RequireAuthorization(QmsPolicies.RiskManage);
        g.MapGet(
            "/{id:guid}/final-report",
            async (Guid id, IRiskManagementService service, IRiskFinalReportService reports, HttpResponse response, CancellationToken ct) =>
            {
                var details = await service.GetDetailsAsync(id, ct);
                if (details is null) return Results.NotFound();
                try
                {
                    var file = await reports.EnsureGeneratedAsync(details, ct);
                    response.Headers.Append("X-Content-SHA256", file.Sha256);
                    return Results.File(file.Content, "application/pdf", file.FileName);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(statusCode: 409, title: "Nihai FMEA çıktısı üretilemedi", detail: ex.Message);
                }
            });
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
            return Results.Problem(
                statusCode: 400,
                title: "Risk kaydı doğrulanamadı",
                detail: ex.Message
            );
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: 409,
                title: "Risk iş kuralı geçişi engelledi",
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
