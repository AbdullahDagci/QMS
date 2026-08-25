namespace Qms.Domain.Documents;

public sealed class DocumentRevision
{
    private DocumentRevision() { }
    public Guid Id { get; private set; }
    public Guid ControlledDocumentId { get; private set; }
    public int MajorVersion { get; private set; }
    public int MinorVersion { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string ChangeSummary { get; private set; } = string.Empty;
    public string PreparedBy { get; private set; } = string.Empty;
    public DocumentRevisionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? EffectiveAtUtc { get; private set; }
    public string VersionLabel => $"{MajorVersion}.{MinorVersion}";

    internal static DocumentRevision Create(Guid documentId, int major, int minor, string content, string summary, string preparedBy, DateTimeOffset now) { Text(content, nameof(content), 50000); Text(summary, nameof(summary), 2000); Text(preparedBy, nameof(preparedBy), 160); return new() { Id = Guid.CreateVersion7(), ControlledDocumentId = documentId, MajorVersion = major, MinorVersion = minor, Content = content.Trim(), ChangeSummary = summary.Trim(), PreparedBy = preparedBy.Trim(), Status = DocumentRevisionStatus.Draft, CreatedAtUtc = now }; }
    internal void Update(string content, string summary) { if (Status != DocumentRevisionStatus.Draft) throw new InvalidOperationException("İncelemeye gönderilmiş doküman sürümü değiştirilemez."); Text(content, nameof(content), 50000); Text(summary, nameof(summary), 2000); Content = content.Trim(); ChangeSummary = summary.Trim(); }
    internal void StartReview() { if (Status != DocumentRevisionStatus.Draft) throw new InvalidOperationException("Sürüm incelemeye uygun değil."); Status = DocumentRevisionStatus.InReview; }
    internal void Approve(DateTimeOffset now) { if (Status != DocumentRevisionStatus.InReview) throw new InvalidOperationException("Yalnız incelenen sürüm onaylanabilir."); Status = DocumentRevisionStatus.Approved; ApprovedAtUtc = now; }
    internal void MakeEffective(DateTimeOffset now) { if (Status != DocumentRevisionStatus.Approved) throw new InvalidOperationException("Yalnız onaylı sürüm yürürlüğe alınabilir."); Status = DocumentRevisionStatus.Effective; EffectiveAtUtc = now; }
    internal void Supersede() { if (Status == DocumentRevisionStatus.Effective) Status = DocumentRevisionStatus.Superseded; }
    internal void Withdraw() { if (Status is DocumentRevisionStatus.Effective or DocumentRevisionStatus.Approved) Status = DocumentRevisionStatus.Withdrawn; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
