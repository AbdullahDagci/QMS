namespace Qms.Domain.Documents;

public sealed class DocumentReadReceipt
{
    private DocumentReadReceipt() { }
    public Guid Id { get; private set; }
    public Guid ControlledDocumentId { get; private set; }
    public Guid RevisionId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserDisplayName { get; private set; } = string.Empty;
    public string SignatureMeaning { get; private set; } = string.Empty;
    public DateTimeOffset AcknowledgedAtUtc { get; private set; }
    internal static DocumentReadReceipt Create(Guid documentId, Guid revisionId, Guid userId, string displayName, string meaning, DateTimeOffset now) { if (userId == Guid.Empty) throw new ArgumentException("Kullanıcı zorunludur."); ArgumentException.ThrowIfNullOrWhiteSpace(displayName); ArgumentException.ThrowIfNullOrWhiteSpace(meaning); return new() { Id = Guid.CreateVersion7(), ControlledDocumentId = documentId, RevisionId = revisionId, UserId = userId, UserDisplayName = displayName.Trim(), SignatureMeaning = meaning.Trim(), AcknowledgedAtUtc = now }; }
}
