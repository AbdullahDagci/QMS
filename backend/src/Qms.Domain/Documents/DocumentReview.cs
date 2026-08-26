namespace Qms.Domain.Documents;

public sealed class DocumentReview
{
    private DocumentReview() { }
    public Guid Id { get; private set; }
    public Guid ControlledDocumentId { get; private set; }
    public Guid RevisionId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string Reviewer { get; private set; } = string.Empty;
    public Guid? ReviewerUserId { get; private set; }
    public DocumentReviewStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    internal static DocumentReview Create(Guid documentId, Guid revisionId, Guid departmentId, string department, Guid reviewerUserId, string reviewer) { Text(department, nameof(department), 120); Text(reviewer, nameof(reviewer), 160); return new() { Id = Guid.CreateVersion7(), ControlledDocumentId = documentId, RevisionId = revisionId, DepartmentId = departmentId, Department = department.Trim(), ReviewerUserId = reviewerUserId, Reviewer = reviewer.Trim(), Status = DocumentReviewStatus.Pending }; }
    internal void Complete(bool approved, string comment, DateTimeOffset now) { if (Status != DocumentReviewStatus.Pending) throw new InvalidOperationException("Doküman incelemesi daha önce tamamlanmış."); Text(comment, nameof(comment), 2000); Status = approved ? DocumentReviewStatus.Approved : DocumentReviewStatus.ChangesRequested; Comment = comment.Trim(); CompletedAtUtc = now; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
