using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Contracts.ElectronicSignatures;

namespace Qms.Api.Endpoints;

public static class ElectronicSignatureEndpoints
{
    public static IEndpointRouteBuilder MapElectronicSignatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/e-signatures")
            .WithTags("Electronic Signatures")
            .RequireAuthorization(QmsPolicies.QualityView);

        group.MapGet("/{id:guid}/verification", async (
            Guid id,
            IElectronicSignatureService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.VerifyAsync(id, cancellationToken);
                return Results.Ok(new ElectronicSignatureVerificationResponse(
                    result.SignatureId,
                    result.IsValid,
                    result.ProviderType,
                    result.SignatureMethod,
                    result.AggregateType,
                    result.AggregateId,
                    result.RecordVersion,
                    result.Operation,
                    result.Meaning,
                    result.Signer,
                    result.SignedAtUtc,
                    result.ContentHash,
                    result.VerificationMessage));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });

        return endpoints;
    }
}
