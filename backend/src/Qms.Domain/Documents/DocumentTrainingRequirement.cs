namespace Qms.Domain.Documents;

public sealed class DocumentTrainingRequirement
{
    private DocumentTrainingRequirement() { }
    public Guid Id { get; private set; }
    public Guid ControlledDocumentId { get; private set; }
    public Guid RevisionId { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public string AssignedUser { get; private set; } = string.Empty;
    public DocumentTrainingStatus Status { get; private set; }
    public string? Evidence { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    internal static DocumentTrainingRequirement Create(Guid documentId, Guid revisionId, string position, string assignedUser) { Text(position, nameof(position), 160); Text(assignedUser, nameof(assignedUser), 160); return new() { Id = Guid.CreateVersion7(), ControlledDocumentId = documentId, RevisionId = revisionId, Position = position.Trim(), AssignedUser = assignedUser.Trim(), Status = DocumentTrainingStatus.Pending }; }
    internal void Complete(string evidence, DateTimeOffset now) { if (Status == DocumentTrainingStatus.Completed) throw new InvalidOperationException("Eğitim gerekliliği daha önce tamamlanmış."); Text(evidence, nameof(evidence), 1000); Status = DocumentTrainingStatus.Completed; Evidence = evidence.Trim(); CompletedAtUtc = now; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
