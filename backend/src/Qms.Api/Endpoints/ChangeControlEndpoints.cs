using Microsoft.EntityFrameworkCore;
using Qms.Application.ChangeControls;
using Qms.Application.Security;
using Qms.Contracts.ChangeControls;

namespace Qms.Api.Endpoints;

public static class ChangeControlEndpoints
{
    public static IEndpointRouteBuilder MapChangeControlEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/change-controls").WithTags("Change Controls").RequireAuthorization(QmsPolicies.QualityView);
        group.MapPost("/search", async (ChangeControlSearchRequest request, IChangeControlService service, CancellationToken ct) => await Execute(() => service.SearchAsync(request, ct)));
        group.MapGet("/{id:guid}/details", async (Guid id, IChangeControlService service, CancellationToken ct) => { var result = await service.GetDetailsAsync(id, ct); return result is null ? Results.NotFound() : Results.Ok(result); });
        group.MapPost("/", async (CreateChangeControlRequest request, IChangeControlService service, CancellationToken ct) => await Execute(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.ChangeCreate);
        group.MapPost("/{id:guid}/assessments/{assessmentId:guid}/complete", async (Guid id, Guid assessmentId, CompleteChangeAssessmentRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.CompleteAssessmentAsync(id, assessmentId, request, ct))).RequireAuthorization(QmsPolicies.ChangeReview);
        group.MapPost("/{id:guid}/actions", async (Guid id, AddChangeActionRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.AddActionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ChangeReview);
        group.MapPost("/{id:guid}/actions/{actionId:guid}/complete", async (Guid id, Guid actionId, CompleteChangeActionRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.CompleteActionAsync(id, actionId, request, ct))).RequireAuthorization(QmsPolicies.ChangeExecute);
        group.MapPost("/{id:guid}/actions/{actionId:guid}/verify", async (Guid id, Guid actionId, VerifyChangeActionRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.VerifyActionAsync(id, actionId, request, ct))).RequireAuthorization(QmsPolicies.ChangeReview);
        group.MapPost("/{id:guid}/authority-approval", async (Guid id, SetAuthorityApprovalRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.SetAuthorityApprovalAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ChangeReview);
        group.MapPost("/{id:guid}/transitions", async (Guid id, TransitionChangeControlRequest request, IChangeControlService service, CancellationToken ct) => await Mutate(() => service.TransitionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.ChangeApprove);
        return endpoints;
    }

    private static async Task<IResult> Execute<T>(Func<Task<T>> action, bool created = false)
    {
        try { var result = await action(); return created ? Results.Json(result, statusCode: StatusCodes.Status201Created) : Results.Ok(result); }
        catch (ArgumentException ex) { return Results.Problem(statusCode: 400, title: "Değişiklik kaydı doğrulanamadı", detail: ex.Message); }
        catch (InvalidOperationException ex) { return Results.Problem(statusCode: 409, title: "İş kuralı geçişi engelledi", detail: ex.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti. Ekranı yenileyip tekrar deneyin."); }
    }
    private static async Task<IResult> Mutate(Func<Task<ChangeControlDetailsResponse?>> action) { var result = await Execute(action); return result; }
}
