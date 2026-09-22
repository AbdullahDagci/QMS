namespace Qms.Application.Files;

public interface IManagedFileService
{
    Task<ManagedFileResponse> UploadAsync(string aggregateType, Guid aggregateId, string category,
        string fileName, string contentType, Stream content, DateTimeOffset? retainUntilUtc,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ManagedFileResponse>> ListAsync(string aggregateType, Guid aggregateId,
        CancellationToken cancellationToken);
    Task<ManagedFileDownload?> DownloadAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record ManagedFileResponse(Guid Id, string AggregateType, Guid AggregateId,
    string Category, string FileName, string ContentType, long Size, string Sha256,
    string UploadedBy, DateTimeOffset UploadedAtUtc, DateTimeOffset RetainUntilUtc);

public sealed record ManagedFileDownload(byte[] Content, string FileName, string ContentType, string Sha256);
