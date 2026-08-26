namespace Qms.Domain.Documents;

public sealed class ControlledDocument
{
    private readonly List<DocumentRevision> _revisions = [];
    private readonly List<DocumentReview> _reviews = [];
    private readonly List<DocumentTrainingRequirement> _trainingRequirements = [];
    private readonly List<ControlledDocumentCopy> _copies = [];
    private readonly List<DocumentReadReceipt> _readReceipts = [];
    private ControlledDocument() { }
    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid? SourceChangeControlId { get; private set; }
    public string DocumentCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public Guid? DepartmentId { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string Confidentiality { get; private set; } = string.Empty;
    public int ReviewPeriodMonths { get; private set; }
    public DateTimeOffset PlannedEffectiveDateUtc { get; private set; }
    public DateTimeOffset? NextReviewDateUtc { get; private set; }
    public Guid CurrentRevisionId { get; private set; }
    public ControlledDocumentStatus Status { get; private set; }
    public string? WithdrawalReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<DocumentRevision> Revisions => _revisions;
    public IReadOnlyCollection<DocumentReview> Reviews => _reviews;
    public IReadOnlyCollection<DocumentTrainingRequirement> TrainingRequirements => _trainingRequirements;
    public IReadOnlyCollection<ControlledDocumentCopy> Copies => _copies;
    public IReadOnlyCollection<DocumentReadReceipt> ReadReceipts => _readReceipts;
    public DocumentRevision CurrentRevision => _revisions.Single(x => x.Id == CurrentRevisionId);

    public static ControlledDocument Create(Guid qualityRecordId, Guid? sourceChangeId, string documentCode, string title, string type, Guid ownerUserId, string owner, Guid departmentId, string department, string confidentiality, int reviewPeriodMonths, DateTimeOffset effectiveDate, string content, string summary, IEnumerable<(Guid DepartmentId, string Department, Guid ReviewerUserId, string Reviewer)> reviewDepartments, IEnumerable<(Guid PositionId, string Position)> trainingPositions, DateTimeOffset now)
    {
        Text(documentCode, nameof(documentCode), 80); Text(title, nameof(title), 240); Text(type, nameof(type), 80); Text(owner, nameof(owner), 160); Text(department, nameof(department), 120); Text(confidentiality, nameof(confidentiality), 40);
        if (reviewPeriodMonths is < 1 or > 60) throw new ArgumentException("Gözden geçirme periyodu 1–60 ay olmalıdır.");
        var result = new ControlledDocument { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, SourceChangeControlId = sourceChangeId, DocumentCode = documentCode.Trim(), Title = title.Trim(), DocumentType = type.Trim(), OwnerUserId = ownerUserId, Owner = owner.Trim(), DepartmentId = departmentId, Department = department.Trim(), Confidentiality = confidentiality.Trim(), ReviewPeriodMonths = reviewPeriodMonths, PlannedEffectiveDateUtc = effectiveDate, Status = ControlledDocumentStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
        var revision = DocumentRevision.Create(result.Id, 0, 1, content, summary, owner, now); result._revisions.Add(revision); result.CurrentRevisionId = revision.Id;
        result.CreateGates(revision, reviewDepartments, trainingPositions); return result;
    }

    public void UpdateDraft(long version, string content, string summary, DateTimeOffset now) { EnsureVersion(version); if (Status is not (ControlledDocumentStatus.Draft or ControlledDocumentStatus.Writing)) throw new InvalidOperationException("Doküman içeriği yalnız taslak/yazım aşamasında değiştirilebilir."); CurrentRevision.Update(content, summary); Touch(now); }
    public void CompleteReview(long version, Guid reviewId, bool approved, string comment, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ControlledDocumentStatus.Review); Review(reviewId).Complete(approved, comment, now); Touch(now); }
    public void CompleteTraining(long version, Guid requirementId, string evidence, DateTimeOffset now) { EnsureVersion(version); if (Status != ControlledDocumentStatus.TrainingWaiting) throw new InvalidOperationException("Doküman eğitim bekleme aşamasında değil."); Training(requirementId).Complete(evidence, now); Touch(now); }
    public void StartRevision(long version, bool major, string summary, IEnumerable<(Guid DepartmentId, string Department, Guid ReviewerUserId, string Reviewer)> reviewDepartments, IEnumerable<(Guid PositionId, string Position)> trainingPositions, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ControlledDocumentStatus.RevisionPending); Text(summary, nameof(summary), 2000); var previous = CurrentRevision; var next = DocumentRevision.Create(Id, major ? previous.MajorVersion + 1 : previous.MajorVersion, major ? 0 : previous.MinorVersion + 1, previous.Content, summary, previous.PreparedBy, now); _revisions.Add(next); CurrentRevisionId = next.Id; CreateGates(next, reviewDepartments, trainingPositions); Move(ControlledDocumentStatus.Writing, now); }
    public ControlledDocumentCopy IssueCopy(long version, string copyNumber, string recipient, string purpose, DateTimeOffset? dueBack, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ControlledDocumentStatus.Effective); if (_copies.Any(x => x.CopyNumber == copyNumber)) throw new InvalidOperationException("Kontrollü kopya numarası daha önce kullanılmış."); var copy = ControlledDocumentCopy.Create(Id, CurrentRevision.Id, copyNumber, recipient, purpose, dueBack, now); _copies.Add(copy); Touch(now); return copy; }
    public void CloseCopy(long version, Guid copyId, bool destroyed, DateTimeOffset now) { EnsureVersion(version); Copy(copyId).Close(destroyed, now); Touch(now); }
    public DocumentReadReceipt Acknowledge(long version, Guid userId, string displayName, string meaning, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ControlledDocumentStatus.Effective); if (_readReceipts.Any(x => x.RevisionId == CurrentRevision.Id && x.UserId == userId)) throw new InvalidOperationException("Bu sürüm için okuma kanıtı daha önce kaydedilmiş."); var receipt = DocumentReadReceipt.Create(Id, CurrentRevision.Id, userId, displayName, meaning, now); _readReceipts.Add(receipt); Touch(now); return receipt; }

    public void Transition(long version, string transition, string? note, bool requiresRevision, DateTimeOffset now)
    {
        EnsureVersion(version);
        switch (transition.Trim().ToLowerInvariant())
        {
            case "start-writing": EnsureStatus(ControlledDocumentStatus.Draft); Move(ControlledDocumentStatus.Writing, now); break;
            case "submit-review": EnsureStatus(ControlledDocumentStatus.Writing); if (_reviews.Where(x => x.RevisionId == CurrentRevision.Id).Any() is false) throw new InvalidOperationException("En az bir doküman incelemesi tanımlanmalıdır."); CurrentRevision.StartReview(); Move(ControlledDocumentStatus.Review, now); break;
            case "submit-approval": EnsureStatus(ControlledDocumentStatus.Review); var reviews = _reviews.Where(x => x.RevisionId == CurrentRevision.Id).ToList(); if (reviews.Any(x => x.Status == DocumentReviewStatus.Pending)) throw new InvalidOperationException("Tüm doküman incelemeleri tamamlanmalıdır."); if (reviews.Any(x => x.Status == DocumentReviewStatus.ChangesRequested)) throw new InvalidOperationException("Revizyon istenmiş inceleme varken onaya geçilemez."); Move(ControlledDocumentStatus.Approval, now); break;
            case "approve": EnsureStatus(ControlledDocumentStatus.Approval); CurrentRevision.Approve(now); Move(_trainingRequirements.Any(x => x.RevisionId == CurrentRevision.Id && x.Status == DocumentTrainingStatus.Pending) ? ControlledDocumentStatus.TrainingWaiting : ControlledDocumentStatus.Approved, now); break;
            case "release": if (Status is not (ControlledDocumentStatus.Approved or ControlledDocumentStatus.TrainingWaiting)) throw new InvalidOperationException("Doküman yürürlüğe alınmaya uygun değil."); if (_trainingRequirements.Any(x => x.RevisionId == CurrentRevision.Id && x.Status == DocumentTrainingStatus.Pending)) throw new InvalidOperationException("Zorunlu eğitimler tamamlanmadan doküman yürürlüğe alınamaz."); if (PlannedEffectiveDateUtc > now) throw new InvalidOperationException("Planlanan yürürlük tarihi henüz gelmedi."); foreach (var revision in _revisions.Where(x => x.Id != CurrentRevision.Id)) revision.Supersede(); CurrentRevision.MakeEffective(now); NextReviewDateUtc = now.AddMonths(ReviewPeriodMonths); Move(ControlledDocumentStatus.Effective, now); break;
            case "request-revision": EnsureStatus(ControlledDocumentStatus.Effective); Move(ControlledDocumentStatus.RevisionPending, now); break;
            case "start-periodic-review": EnsureStatus(ControlledDocumentStatus.Effective); Move(ControlledDocumentStatus.PeriodicReview, now); break;
            case "complete-periodic-review": EnsureStatus(ControlledDocumentStatus.PeriodicReview); Text(note ?? string.Empty, nameof(note), 2000); if (requiresRevision) Move(ControlledDocumentStatus.RevisionPending, now); else { NextReviewDateUtc = now.AddMonths(ReviewPeriodMonths); Move(ControlledDocumentStatus.Effective, now); } break;
            case "withdraw": EnsureStatus(ControlledDocumentStatus.Effective); Text(note ?? string.Empty, nameof(note), 2000); WithdrawalReason = note!.Trim(); CurrentRevision.Withdraw(); Move(ControlledDocumentStatus.Withdrawn, now); break;
            case "archive": EnsureStatus(ControlledDocumentStatus.Withdrawn); if (_copies.Any(x => x.Status == ControlledCopyStatus.Issued)) throw new InvalidOperationException("İade edilmemiş kontrollü kopya varken doküman arşivlenemez."); ArchivedAtUtc = now; Move(ControlledDocumentStatus.Archived, now); break;
            default: throw new ArgumentException("Bilinmeyen doküman durum geçişi.");
        }
    }

    private void CreateGates(DocumentRevision revision, IEnumerable<(Guid DepartmentId, string Department, Guid ReviewerUserId, string Reviewer)> reviews, IEnumerable<(Guid PositionId, string Position)> trainings) { foreach (var review in reviews.GroupBy(x => x.DepartmentId).Select(x => x.First())) _reviews.Add(DocumentReview.Create(Id, revision.Id, review.DepartmentId, review.Department, review.ReviewerUserId, review.Reviewer)); foreach (var training in trainings.GroupBy(x => x.PositionId).Select(x => x.First())) _trainingRequirements.Add(DocumentTrainingRequirement.Create(Id, revision.Id, training.PositionId, training.Position)); }
    private DocumentReview Review(Guid id) => _reviews.SingleOrDefault(x => x.Id == id && x.RevisionId == CurrentRevision.Id) ?? throw new ArgumentException("Doküman incelemesi bulunamadı.");
    private DocumentTrainingRequirement Training(Guid id) => _trainingRequirements.SingleOrDefault(x => x.Id == id && x.RevisionId == CurrentRevision.Id) ?? throw new ArgumentException("Eğitim gerekliliği bulunamadı.");
    private ControlledDocumentCopy Copy(Guid id) => _copies.SingleOrDefault(x => x.Id == id) ?? throw new ArgumentException("Kontrollü kopya bulunamadı.");
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("Doküman başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void EnsureStatus(ControlledDocumentStatus status) { if (Status != status) throw new InvalidOperationException($"Geçersiz doküman geçişi. Beklenen: {status}, mevcut: {Status}."); }
    private void Move(ControlledDocumentStatus status, DateTimeOffset now) { Status = status; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
