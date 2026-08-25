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
