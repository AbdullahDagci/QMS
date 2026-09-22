namespace Qms.Domain.Files;

public sealed class ManagedFile
{
    private ManagedFile() { }

    public Guid Id { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string IntegrityMac { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public string UploadedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }
    public DateTimeOffset RetainUntilUtc { get; private set; }

    public static ManagedFile Create(Guid id, string aggregateType, Guid aggregateId,
        string category, string originalFileName, string storagePath, string contentType,
        long size, string contentHash, string integrityMac, Guid uploadedByUserId,
        string uploadedByDisplayName, DateTimeOffset uploadedAtUtc, DateTimeOffset retainUntilUtc)
    {
        if (id == Guid.Empty || aggregateId == Guid.Empty || uploadedByUserId == Guid.Empty)
            throw new ArgumentException("Dosya kimlikleri geçersizdir.");
        foreach (var value in new[] { aggregateType, category, originalFileName, storagePath,
                     contentType, contentHash, integrityMac, uploadedByDisplayName })
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (size <= 0) throw new ArgumentException("Boş dosya yüklenemez.");
        if (retainUntilUtc <= uploadedAtUtc) throw new ArgumentException("Saklama süresi gelecekte olmalıdır.");
        return new ManagedFile
        {
            Id = id,
            AggregateType = aggregateType.Trim(),
            AggregateId = aggregateId,
            Category = category.Trim(),
            OriginalFileName = originalFileName.Trim(),
            StoragePath = storagePath.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            Size = size,
            ContentHash = contentHash,
            IntegrityMac = integrityMac,
            UploadedByUserId = uploadedByUserId,
            UploadedByDisplayName = uploadedByDisplayName.Trim(),
            UploadedAtUtc = uploadedAtUtc,
            RetainUntilUtc = retainUntilUtc
        };
    }
}
