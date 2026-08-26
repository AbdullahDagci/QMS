using Qms.Domain.ElectronicSignatures;

namespace Qms.Application.ElectronicSignatures;

public interface IElectronicSignatureService
{
    Task AuthenticateAsync(string? password, bool meaningAccepted, CancellationToken cancellationToken);

    ElectronicSignature CreateInternal(
        Guid qualityRecordId,
        string aggregateType,
        Guid aggregateId,
        long recordVersion,
        string operation,
        string meaning,
        object signedContent,
        DateTimeOffset signedAtUtc,
        string? comment = null);

    Task<ElectronicSignatureVerification> VerifyAsync(Guid signatureId, CancellationToken cancellationToken);
}

public sealed record ElectronicSignatureVerification(
    Guid SignatureId,
    bool IsValid,
    string ProviderType,
    string SignatureMethod,
    string AggregateType,
    Guid AggregateId,
    long RecordVersion,
    string Operation,
    string Meaning,
    string Signer,
    DateTimeOffset SignedAtUtc,
    string ContentHash,
    string VerificationMessage);
