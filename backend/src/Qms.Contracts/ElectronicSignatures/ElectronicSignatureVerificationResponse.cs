namespace Qms.Contracts.ElectronicSignatures;

public sealed record ElectronicSignatureVerificationResponse(
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
