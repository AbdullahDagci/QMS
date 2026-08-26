namespace Qms.Domain.ChangeControls;

public sealed class ChangeControl
{
    private readonly List<ChangeAssessment> _assessments = [];
    private readonly List<ChangeImplementationAction> _actions = [];
    private ChangeControl() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid? SourceCapaId { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string CurrentState { get; private set; } = string.Empty;
    public string ProposedState { get; private set; } = string.Empty;
    public string Justification { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public bool IsTemporary { get; private set; }
    public DateTimeOffset? TemporaryUntilUtc { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public DateTimeOffset TargetDateUtc { get; private set; }
    public string RiskLevel { get; private set; } = string.Empty;
    public string RiskSummary { get; private set; } = string.Empty;
    public bool ProductImpact { get; private set; }
    public bool SiteImpact { get; private set; }
    public bool ValidationRequired { get; private set; }
    public string RegulatoryImpact { get; private set; } = string.Empty;
    public string RollbackPlan { get; private set; } = string.Empty;
    public string? AuthorityApprovalReference { get; private set; }
    public DateTimeOffset? CommissionedAtUtc { get; private set; }
    public string? PostImplementationResult { get; private set; }
    public string? ClosureNote { get; private set; }
    public ChangeControlStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<ChangeAssessment> Assessments => _assessments;
    public IReadOnlyCollection<ChangeImplementationAction> Actions => _actions;

    public static ChangeControl Create(Guid qualityRecordId, Guid? sourceCapaId, string changeType, string title, string currentState, string proposedState, string justification, string scope, bool isTemporary, DateTimeOffset? temporaryUntilUtc, Guid ownerUserId, string owner, DateTimeOffset targetDateUtc, string riskLevel, string riskSummary, bool productImpact, bool siteImpact, bool validationRequired, string regulatoryImpact, string rollbackPlan, IEnumerable<(Guid DepartmentId, string Department, Guid ReviewerUserId, string Reviewer)> assessments, DateTimeOffset now)
    {
        Text(changeType, nameof(changeType), 80); Text(title, nameof(title), 240); Text(currentState, nameof(currentState), 4000); Text(proposedState, nameof(proposedState), 4000); Text(justification, nameof(justification), 3000); Text(scope, nameof(scope), 3000); Text(owner, nameof(owner), 160); Text(riskLevel, nameof(riskLevel), 40); Text(riskSummary, nameof(riskSummary), 2000); Text(regulatoryImpact, nameof(regulatoryImpact), 80); Text(rollbackPlan, nameof(rollbackPlan), 3000);
        if (qualityRecordId == Guid.Empty) throw new ArgumentException("Kalite kaydı zorunludur.");
        if (targetDateUtc <= now) throw new ArgumentException("Değişiklik hedef tarihi gelecekte olmalıdır.");
        if (isTemporary && (temporaryUntilUtc is null || temporaryUntilUtc <= now || temporaryUntilUtc > targetDateUtc)) throw new ArgumentException("Geçici değişiklik bitiş tarihi bugün ile hedef tarih arasında olmalıdır.");
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Değişiklik sorumlusu zorunludur.");
        var result = new ChangeControl { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, SourceCapaId = sourceCapaId, ChangeType = changeType.Trim(), Title = title.Trim(), CurrentState = currentState.Trim(), ProposedState = proposedState.Trim(), Justification = justification.Trim(), Scope = scope.Trim(), IsTemporary = isTemporary, TemporaryUntilUtc = isTemporary ? temporaryUntilUtc : null, OwnerUserId = ownerUserId, Owner = owner.Trim(), TargetDateUtc = targetDateUtc, RiskLevel = riskLevel.Trim(), RiskSummary = riskSummary.Trim(), ProductImpact = productImpact, SiteImpact = siteImpact, ValidationRequired = validationRequired, RegulatoryImpact = regulatoryImpact.Trim(), RollbackPlan = rollbackPlan.Trim(), Status = ChangeControlStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
        foreach (var item in assessments.DistinctBy(x => x.DepartmentId)) result._assessments.Add(ChangeAssessment.Create(result.Id, item.DepartmentId, item.Department, item.ReviewerUserId, item.Reviewer));
        return result;
    }

    public void CompleteAssessment(long version, Guid assessmentId, bool approved, string impactSummary, string requiredActions, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ChangeControlStatus.DepartmentReview); Assessment(assessmentId).Complete(approved, impactSummary, requiredActions, now); Touch(now); }
    public void AddAction(long version, string category, string description, Guid ownerUserId, string owner, DateTimeOffset target, bool isBlocking, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ChangeControlStatus.PlanApproval); _actions.Add(ChangeImplementationAction.Create(Id, category, description, ownerUserId, owner, target, isBlocking, now)); Touch(now); }
    public void RequestActionCompletion(long version, Guid actionId, string evidence, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ChangeControlStatus.Implementation); Action(actionId).RequestCompletion(evidence, now); Touch(now); }
    public void VerifyAction(long version, Guid actionId, bool approved, string note, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(ChangeControlStatus.Implementation); Action(actionId).Verify(approved, note, now); Touch(now); }
    public void SetAuthorityApproval(long version, string reference, DateTimeOffset now) { EnsureVersion(version); if (Status is ChangeControlStatus.Closed or ChangeControlStatus.RolledBack or ChangeControlStatus.Voided) throw new InvalidOperationException("Kapanmış değişiklik kaydı güncellenemez."); Text(reference, nameof(reference), 500); AuthorityApprovalReference = reference.Trim(); Touch(now); }

    public void Transition(long version, string transition, string? note, bool successful, DateTimeOffset now)
    {
        EnsureVersion(version);
        switch (transition.Trim().ToLowerInvariant())
        {
            case "submit": EnsureStatus(ChangeControlStatus.Draft); Move(ChangeControlStatus.PreliminaryReview, now); break;
            case "approve-preliminary": EnsureStatus(ChangeControlStatus.PreliminaryReview); if (_assessments.Count == 0) throw new InvalidOperationException("En az bir bölüm değerlendirmesi tanımlanmalıdır."); Move(ChangeControlStatus.DepartmentReview, now); break;
            case "submit-board": EnsureStatus(ChangeControlStatus.DepartmentReview); if (_assessments.Any(x => x.Status == ChangeAssessmentStatus.Pending)) throw new InvalidOperationException("Tüm paralel bölüm değerlendirmeleri tamamlanmalıdır."); if (_assessments.Any(x => x.Status == ChangeAssessmentStatus.Rejected)) throw new InvalidOperationException("Reddedilmiş bölüm değerlendirmesi varken kurul aşamasına geçilemez."); Move(ChangeControlStatus.BoardReview, now); break;
            case "approve-board": EnsureStatus(ChangeControlStatus.BoardReview); Move(ChangeControlStatus.PlanApproval, now); break;
            case "approve-plan": EnsureStatus(ChangeControlStatus.PlanApproval); if (_actions.Count == 0) throw new InvalidOperationException("En az bir uygulama aksiyonu planlanmalıdır."); Move(ChangeControlStatus.Implementation, now); break;
            case "request-commissioning": EnsureStatus(ChangeControlStatus.Implementation); if (_actions.Any(x => x.Status != ChangeImplementationActionStatus.Verified)) throw new InvalidOperationException("Tüm uygulama aksiyonları kanıtla tamamlanıp doğrulanmalıdır."); if (RequiresAuthorityApproval() && string.IsNullOrWhiteSpace(AuthorityApprovalReference)) throw new InvalidOperationException("Otorite onay belgesi olmadan devreye alma onayına geçilemez."); Move(ChangeControlStatus.CommissioningApproval, now); break;
            case "commission": EnsureStatus(ChangeControlStatus.CommissioningApproval); CommissionedAtUtc = now; Move(ChangeControlStatus.PostImplementationVerification, now); break;
            case "verify-implementation": EnsureStatus(ChangeControlStatus.PostImplementationVerification); Text(note ?? string.Empty, nameof(note), 3000); PostImplementationResult = note!.Trim(); Move(successful ? ChangeControlStatus.ClosureApproval : ChangeControlStatus.RolledBack, now); break;
            case "close": EnsureStatus(ChangeControlStatus.ClosureApproval); Text(note ?? string.Empty, nameof(note), 2000); ClosureNote = note!.Trim(); ClosedAtUtc = now; Move(ChangeControlStatus.Closed, now); break;
            case "rollback": if (Status is not (ChangeControlStatus.Implementation or ChangeControlStatus.CommissioningApproval or ChangeControlStatus.PostImplementationVerification)) throw new InvalidOperationException("Bu aşamada kontrollü geri dönüş başlatılamaz."); Text(note ?? string.Empty, nameof(note), 3000); PostImplementationResult = $"Geri dönüş: {note!.Trim()}"; Move(ChangeControlStatus.RolledBack, now); break;
            default: throw new ArgumentException("Bilinmeyen değişiklik kontrol durum geçişi.");
        }
    }

    private bool RequiresAuthorityApproval() => RegulatoryImpact.Equals("AuthorityApproval", StringComparison.OrdinalIgnoreCase);
    private ChangeAssessment Assessment(Guid id) => _assessments.SingleOrDefault(x => x.Id == id) ?? throw new ArgumentException("Bölüm değerlendirmesi bulunamadı.");
    private ChangeImplementationAction Action(Guid id) => _actions.SingleOrDefault(x => x.Id == id) ?? throw new ArgumentException("Uygulama aksiyonu bulunamadı.");
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("Değişiklik kaydı başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void EnsureStatus(ChangeControlStatus status) { if (Status != status) throw new InvalidOperationException($"Geçersiz değişiklik geçişi. Beklenen: {status}, mevcut: {Status}."); }
    private void Move(ChangeControlStatus status, DateTimeOffset now) { Status = status; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
