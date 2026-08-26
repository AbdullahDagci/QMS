namespace Qms.Domain.WorkItems;

public enum WorkItemStatus { Draft, Assigned, InProgress, PendingVerification, Completed, Cancelled }

public sealed class WorkItem
{
    private WorkItem() { }
    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public string? SourceModule { get; private set; }
    public Guid? SourceRecordId { get; private set; }
    public string? SourceRecordNumber { get; private set; }
    public string Category { get; private set; } = "";
    public string Priority { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public Guid OwnerUserId { get; private set; }
    public string Owner { get; private set; } = "";
    public Guid? OwnerDepartmentId { get; private set; }
    public string? OwnerDepartment { get; private set; }
    public Guid VerifierUserId { get; private set; }
    public string Verifier { get; private set; } = "";
    public DateTimeOffset DueAtUtc { get; private set; }
    public string? CompletionEvidence { get; private set; }
    public string? VerificationNote { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public long Version { get; private set; }

    public static WorkItem Create(Guid qualityRecordId, string? sourceModule, Guid? sourceRecordId, string? sourceRecordNumber, string category, string priority, string title, string description, Guid ownerUserId, string owner, Guid? ownerDepartmentId, string? ownerDepartment, Guid verifierUserId, string verifier, DateTimeOffset dueAtUtc, DateTimeOffset now)
    {
        if (qualityRecordId == Guid.Empty || ownerUserId == Guid.Empty || verifierUserId == Guid.Empty) throw new ArgumentException("Kalite kaydı, sorumlu ve doğrulayıcı zorunludur.");
        Text(category, nameof(category), 64); Text(priority, nameof(priority), 64); Text(title, nameof(title), 240); Text(description, nameof(description), 6000); Text(owner, nameof(owner), 200); Text(verifier, nameof(verifier), 200);
        if (dueAtUtc <= now) throw new ArgumentException("Hedef tarih gelecekte olmalıdır.");
        if (sourceRecordId.HasValue && string.IsNullOrWhiteSpace(sourceModule)) throw new ArgumentException("Kaynak kayıt seçildiğinde kaynak modül zorunludur.");
        return new() { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, SourceModule = Clean(sourceModule, 20), SourceRecordId = sourceRecordId, SourceRecordNumber = Clean(sourceRecordNumber, 80), Category = category.Trim(), Priority = priority.Trim(), Title = title.Trim(), Description = description.Trim(), OwnerUserId = ownerUserId, Owner = owner.Trim(), OwnerDepartmentId = ownerDepartmentId, OwnerDepartment = Clean(ownerDepartment, 160), VerifierUserId = verifierUserId, Verifier = verifier.Trim(), DueAtUtc = dueAtUtc, Status = WorkItemStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
    }
    public void Assign(long version, DateTimeOffset now) { Ensure(version, WorkItemStatus.Draft); Move(WorkItemStatus.Assigned, now); }
    public void Start(long version, DateTimeOffset now) { Ensure(version, WorkItemStatus.Assigned); StartedAtUtc = now; Move(WorkItemStatus.InProgress, now); }
    public void Submit(long version, string evidence, DateTimeOffset now) { Ensure(version, WorkItemStatus.InProgress); Text(evidence, nameof(evidence), 6000); CompletionEvidence = evidence.Trim(); SubmittedAtUtc = now; Move(WorkItemStatus.PendingVerification, now); }
    public void Verify(long version, bool approved, string note, DateTimeOffset now) { Ensure(version, WorkItemStatus.PendingVerification); Text(note, nameof(note), 4000); VerificationNote = note.Trim(); if (approved) { CompletedAtUtc = now; Move(WorkItemStatus.Completed, now); } else Move(WorkItemStatus.InProgress, now); }
    public void Cancel(long version, string reason, DateTimeOffset now) { if (Status is WorkItemStatus.Completed or WorkItemStatus.Cancelled) throw new InvalidOperationException("Tamamlanan veya iptal edilen iş değiştirilemez."); EnsureVersion(version); Text(reason, nameof(reason), 2000); VerificationNote = reason.Trim(); CancelledAtUtc = now; Move(WorkItemStatus.Cancelled, now); }
    private void Ensure(long version, WorkItemStatus status) { EnsureVersion(version); if (Status != status) throw new InvalidOperationException($"Geçersiz iş aşaması. Beklenen: {status}, mevcut: {Status}."); }
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("İş kaydı başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void Move(WorkItemStatus status, DateTimeOffset now) { Status = status; UpdatedAtUtc = now; Version++; }
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; if (value.Trim().Length > max) throw new ArgumentException($"Metin en fazla {max} karakter olabilir."); return value.Trim(); }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
