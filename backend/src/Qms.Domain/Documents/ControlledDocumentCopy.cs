namespace Qms.Domain.Documents;

public sealed class ControlledDocumentCopy
{
    private ControlledDocumentCopy() { }
    public Guid Id { get; private set; }
    public Guid ControlledDocumentId { get; private set; }
    public Guid RevisionId { get; private set; }
    public string CopyNumber { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public ControlledCopyStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? DueBackAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }
    public DateTimeOffset? DestroyedAtUtc { get; private set; }
    internal static ControlledDocumentCopy Create(Guid documentId, Guid revisionId, string copyNumber, string recipient, string purpose, DateTimeOffset? dueBack, DateTimeOffset now) { Text(copyNumber, nameof(copyNumber), 80); Text(recipient, nameof(recipient), 200); Text(purpose, nameof(purpose), 500); return new() { Id = Guid.CreateVersion7(), ControlledDocumentId = documentId, RevisionId = revisionId, CopyNumber = copyNumber.Trim(), Recipient = recipient.Trim(), Purpose = purpose.Trim(), Status = ControlledCopyStatus.Issued, IssuedAtUtc = now, DueBackAtUtc = dueBack }; }
    internal void Close(bool destroyed, DateTimeOffset now) { if (Status != ControlledCopyStatus.Issued) throw new InvalidOperationException("Kontrollü kopya zaten kapatılmış."); Status = destroyed ? ControlledCopyStatus.Destroyed : ControlledCopyStatus.Returned; if (destroyed) DestroyedAtUtc = now; else ReturnedAtUtc = now; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
