using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Application.Trainings;
using Qms.Contracts.Trainings;

namespace Qms.Api.Endpoints;

public static class TrainingEndpoints
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/trainings").WithTags("Trainings").RequireAuthorization(QmsPolicies.TrainingView);
        group.MapPost("/search", async (TrainingSearchRequest request, ITrainingService service, CancellationToken ct) => await Run(() => service.SearchAsync(request, ct)));
        group.MapGet("/options", async (ITrainingService service, CancellationToken ct) => Results.Ok(await service.GetOptionsAsync(ct)));
        group.MapGet("/matrix", async (ITrainingService service, CancellationToken ct) => Results.Ok(await service.GetMatrixAsync(ct)));
        group.MapPost("/matrix", async (CreateTrainingMatrixRuleRequest request, ITrainingService service, CancellationToken ct) => await Run(() => service.CreateMatrixRuleAsync(request, ct), true)).RequireAuthorization(QmsPolicies.TrainingManage);
        group.MapGet("/{id:guid}/details", async (Guid id, ITrainingService service, CancellationToken ct) => await service.GetDetailsAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound());
        group.MapPost("/", async (CreateTrainingAssignmentRequest request, ITrainingService service, CancellationToken ct) => await Run(() => service.CreateAsync(request, ct), true)).RequireAuthorization(QmsPolicies.TrainingManage);
        group.MapPost("/{id:guid}/transitions", async (Guid id, TransitionTrainingRequest request, ITrainingService service, CancellationToken ct) => await Mutate(() => service.TransitionAsync(id, request, ct))).RequireAuthorization(QmsPolicies.TrainingComplete);
        group.MapPost("/{id:guid}/acknowledge", async (Guid id, AcknowledgeTrainingRequest request, ITrainingService service, CancellationToken ct) => await Mutate(() => service.AcknowledgeAsync(id, request, ct))).RequireAuthorization(QmsPolicies.TrainingComplete);
        group.MapPost("/{id:guid}/assessment", async (Guid id, RecordTrainingAssessmentRequest request, ITrainingService service, CancellationToken ct) => await Mutate(() => service.RecordAssessmentAsync(id, request, ct))).RequireAuthorization(QmsPolicies.TrainingApprove);
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, bool created = false)
    {
        try { var value = await action(); return created ? Results.Json(value, statusCode: 201) : Results.Ok(value); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: "Eğitim kaydı doğrulanamadı", detail: exception.Message); }
        catch (InvalidOperationException exception) { return Results.Problem(statusCode: 409, title: "Eğitim iş kuralı işlemi engelledi", detail: exception.Message); }
        catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "Eşzamanlı güncelleme çakışması", detail: "Kayıt değişti. Ekranı yenileyip tekrar deneyin."); }
    }

    private static Task<IResult> Mutate(Func<Task<TrainingDetailsResponse?>> action) => Run(action);
}
