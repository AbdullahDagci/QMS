namespace Qms.Domain.Capas;

public sealed class Capa
{
    private readonly List<CapaAction> _actions = [];
    private Capa() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid? SourceDeviationId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string RootCause { get; private set; } = string.Empty;
    public string ImmediateActions { get; private set; } = string.Empty;
    public string Owner { get; private set; } = string.Empty;
    public DateTimeOffset TargetDateUtc { get; private set; }
    public bool EffectivenessRequired { get; private set; }
    public string EffectivenessMethod { get; private set; } = string.Empty;
    public string EffectivenessSample { get; private set; } = string.Empty;
    public int ObservationPeriodDays { get; private set; }
    public string SuccessCriteria { get; private set; } = string.Empty;
    public string EffectivenessEvaluator { get; private set; } = string.Empty;
    public DateTimeOffset? EffectivenessDueDateUtc { get; private set; }
    public bool? IsEffective { get; private set; }
    public string? EffectivenessResult { get; private set; }
    public string? ClosureNote { get; private set; }
    public CapaStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CapaAction> Actions => _actions;

    public static Capa Create(Guid qualityRecordId, Guid? sourceDeviationId, string sourceType, string title, string description, string rootCause, string immediateActions, string owner, DateTimeOffset targetDateUtc, bool effectivenessRequired, string method, string sample, int observationDays, string criteria, string evaluator, DateTimeOffset now)
    {
        Text(sourceType, nameof(sourceType), 80); Text(title, nameof(title), 200); Text(description, nameof(description), 4000); Text(rootCause, nameof(rootCause), 4000); Text(immediateActions, nameof(immediateActions), 2000); Text(owner, nameof(owner), 160);
        if (qualityRecordId == Guid.Empty) throw new ArgumentException("Kalite kaydı zorunludur.");
        if (targetDateUtc <= now) throw new ArgumentException("DÖF hedef tarihi gelecekte olmalıdır.");
        if (effectivenessRequired) { Text(method, nameof(method), 1000); Text(sample, nameof(sample), 1000); Text(criteria, nameof(criteria), 2000); Text(evaluator, nameof(evaluator), 160); if (observationDays < 1) throw new ArgumentException("Gözlem süresi en az bir gün olmalıdır."); }
        return new Capa { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, SourceDeviationId = sourceDeviationId, SourceType = sourceType.Trim(), Title = title.Trim(), Description = description.Trim(), RootCause = rootCause.Trim(), ImmediateActions = immediateActions.Trim(), Owner = owner.Trim(), TargetDateUtc = targetDateUtc, EffectivenessRequired = effectivenessRequired, EffectivenessMethod = method.Trim(), EffectivenessSample = sample.Trim(), ObservationPeriodDays = observationDays, SuccessCriteria = criteria.Trim(), EffectivenessEvaluator = evaluator.Trim(), Status = CapaStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
    }

    public void AddAction(long version, string type, string description, string owner, DateTimeOffset target, DateTimeOffset now) { EnsureVersion(version); if (Status is not (CapaStatus.Draft or CapaStatus.ActionPlanning)) throw new InvalidOperationException("Aksiyon yalnız taslak veya aksiyon planı aşamasında eklenebilir."); _actions.Add(CapaAction.Create(Id, type, description, owner, target, now)); Touch(now); }
    public void RequestActionCompletion(long version, Guid actionId, string evidence, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(CapaStatus.Implementation); Action(actionId).RequestCompletion(evidence, now); Touch(now); }
    public void VerifyAction(long version, Guid actionId, bool approved, string note, DateTimeOffset now) { EnsureVersion(version); EnsureStatus(CapaStatus.ActionVerification); Action(actionId).Verify(approved, note, now); Touch(now); }

    public void Transition(long version, string transition, string? note, bool isEffective, DateTimeOffset now)
    {
        EnsureVersion(version);
        switch (transition.Trim().ToLowerInvariant())
        {
            case "submit": EnsureStatus(CapaStatus.Draft); Move(CapaStatus.ScopeApproval, now); break;
            case "approve-scope": EnsureStatus(CapaStatus.ScopeApproval); Move(CapaStatus.RootCauseApproval, now); break;
            case "approve-root-cause": EnsureStatus(CapaStatus.RootCauseApproval); Move(CapaStatus.ActionPlanning, now); break;
            case "submit-plan": EnsureStatus(CapaStatus.ActionPlanning); RequireActions(); Move(CapaStatus.PlanApproval, now); break;
            case "approve-plan": EnsureStatus(CapaStatus.PlanApproval); RequireActions(); Move(CapaStatus.Implementation, now); break;
            case "request-action-verification": EnsureStatus(CapaStatus.Implementation); if (_actions.Any(x => x.Status != CapaActionStatus.CompletionRequested)) throw new InvalidOperationException("Tüm aksiyonların kanıtla tamamlanması gerekir."); Move(CapaStatus.ActionVerification, now); break;
            case "approve-actions": EnsureStatus(CapaStatus.ActionVerification); if (_actions.Any(x => x.Status != CapaActionStatus.Verified)) throw new InvalidOperationException("Tüm aksiyonlar KG tarafından doğrulanmalıdır."); if (EffectivenessRequired) { EffectivenessDueDateUtc = now.AddDays(ObservationPeriodDays); Move(CapaStatus.EffectivenessWaiting, now); } else Move(CapaStatus.ClosureApproval, now); break;
            case "start-effectiveness-review": EnsureStatus(CapaStatus.EffectivenessWaiting); if (EffectivenessDueDateUtc > now) throw new InvalidOperationException("Etkinlik gözlem dönemi henüz tamamlanmadı."); Move(CapaStatus.EffectivenessReview, now); break;
            case "complete-effectiveness": EnsureStatus(CapaStatus.EffectivenessReview); Text(note ?? string.Empty, nameof(note), 4000); IsEffective = isEffective; EffectivenessResult = note!.Trim(); Move(isEffective ? CapaStatus.ClosureApproval : CapaStatus.ActionPlanning, now); break;
            case "close": EnsureStatus(CapaStatus.ClosureApproval); Text(note ?? string.Empty, nameof(note), 2000); ClosureNote = note!.Trim(); ClosedAtUtc = now; Move(CapaStatus.Closed, now); break;
            default: throw new ArgumentException("Bilinmeyen DÖF durum geçişi.");
        }
    }

    private CapaAction Action(Guid id) => _actions.SingleOrDefault(x => x.Id == id) ?? throw new ArgumentException("DÖF aksiyonu bulunamadı.");
    private void RequireActions() { if (_actions.Count == 0) throw new InvalidOperationException("En az bir DÖF aksiyonu planlanmalıdır."); }
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("DÖF başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void EnsureStatus(CapaStatus status) { if (Status != status) throw new InvalidOperationException($"Geçersiz DÖF geçişi. Beklenen: {status}, mevcut: {Status}."); }
    private void Move(CapaStatus status, DateTimeOffset now) { Status = status; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
