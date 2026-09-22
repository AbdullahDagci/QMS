using Qms.Application.Files;
using Qms.Application.Security;
using Qms.Infrastructure.Integrity;

namespace Qms.Api.Endpoints;

public static class ManagedFileEndpoints
{
    public static IEndpointRouteBuilder MapManagedFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/files").RequireAuthorization(QmsPolicies.QualityView)
            .WithTags("Managed Files");

        group.MapPost("/{aggregateType}/{aggregateId:guid}", async (string aggregateType,
            Guid aggregateId, HttpRequest request, IManagedFileService service, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.Problem(statusCode: 415, title: "multipart/form-data gereklidir");
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null) return Results.Problem(statusCode: 400, title: "file alanı zorunludur");
            var category = form["category"].FirstOrDefault() ?? "evidence";
            DateTimeOffset? retainUntil = DateTimeOffset.TryParse(form["retainUntilUtc"].FirstOrDefault(), out var parsed)
                ? parsed : null;
            await using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(aggregateType, aggregateId, category,
                file.FileName, file.ContentType, stream, retainUntil, ct);
            return Results.Created($"/api/v1/files/{result.Id}", result);
        }).DisableAntiforgery();

        group.MapGet("/{aggregateType}/{aggregateId:guid}", (string aggregateType, Guid aggregateId,
            IManagedFileService service, CancellationToken ct) => service.ListAsync(aggregateType, aggregateId, ct));

        group.MapGet("/{id:guid}/download", async (Guid id, IManagedFileService service, CancellationToken ct) =>
        {
            var file = await service.DownloadAsync(id, ct);
            return file is null ? Results.NotFound() : Results.File(file.Content, file.ContentType,
                file.FileName, enableRangeProcessing: false);
        });

        endpoints.MapGet("/api/v1/audit-integrity/{aggregateType}/{aggregateId:guid}",
            async (string aggregateType, Guid aggregateId, AuditIntegrityVerifier verifier, CancellationToken ct) =>
            {
                var result = await verifier.VerifyAsync(aggregateType, aggregateId, ct);
                return result.IsValid ? Results.Ok(result) : Results.Conflict(result);
            })
            .RequireAuthorization(QmsPolicies.AdministrationManage).WithTags("Audit Integrity");
        return endpoints;
    }
}
