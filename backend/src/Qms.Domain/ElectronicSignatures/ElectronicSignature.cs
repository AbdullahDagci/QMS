using System.Text.Json;

namespace Qms.Domain.ElectronicSignatures;

public sealed class ElectronicSignature
{
    private ElectronicSignature()
    {
    }

    public Guid Id { get; private set; }

    public Guid QualityRecordId { get; private set; }

    public long RecordVersion { get; private set; }

    public Guid SignerUserId { get; private set; }

    public string SignerDisplayNameSnapshot { get; private set; } = string.Empty;

    public string Meaning { get; private set; } = string.Empty;

    public DateTimeOffset SignedAtUtc { get; private set; }

    public string? Comment { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    public string IntegrityMac { get; private set; } = string.Empty;

    public string ProviderType { get; private set; } = "Legacy";

    public string SignatureMethod { get; private set; } = "LegacyApplicationSignature";

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public string Operation { get; private set; } = string.Empty;

    public JsonDocument SignedSnapshot { get; private set; } = JsonDocument.Parse("{}");

    public static ElectronicSignature CreateInternal(
        Guid qualityRecordId,
        long recordVersion,
        Guid signerUserId,
        string signerDisplayName,
        string aggregateType,
        Guid aggregateId,
        string operation,
        string meaning,
        DateTimeOffset signedAtUtc,
        JsonDocument signedSnapshot,
        string contentHash,
        string integrityMac,
        string? comment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(signedSnapshot);

        var signature = Create(
            qualityRecordId,
            recordVersion,
            signerUserId,
            signerDisplayName,
            meaning,
            signedAtUtc,
            contentHash,
            comment);
        signature.ProviderType = "Internal";
        signature.SignatureMethod = "PasswordReauthentication";
        signature.AggregateType = aggregateType.Trim();
        signature.AggregateId = aggregateId;
        signature.Operation = operation.Trim().ToLowerInvariant();
        signature.SignedSnapshot = signedSnapshot;
        signature.IntegrityMac = integrityMac;
        return signature;
    }

    public static ElectronicSignature Create(
        Guid qualityRecordId,
        long recordVersion,
        Guid signerUserId,
        string signerDisplayName,
        string meaning,
        DateTimeOffset signedAtUtc,
        string contentHash,
        string? comment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signerDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(meaning);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        return new ElectronicSignature
        {
            Id = Guid.CreateVersion7(),
            QualityRecordId = qualityRecordId,
            RecordVersion = recordVersion,
            SignerUserId = signerUserId,
            SignerDisplayNameSnapshot = signerDisplayName.Trim(),
            Meaning = meaning.Trim(),
            SignedAtUtc = signedAtUtc,
            ContentHash = contentHash.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };
    }
}
