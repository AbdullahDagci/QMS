using System.Security.Cryptography;
using System.Text;

namespace Qms.Domain.SupplierAudits;

public enum SupplierAuditStatus { RiskPlan, ScopeChecklist, AuditorAssignment, Execution, Findings, SupplierResponse, EvidenceVerification, Capa, AuditResult, Closed, Cancelled }
public enum SupplierAuditFindingClassification { Critical, Major, Minor, Observation }
public enum SupplierAuditFindingStatus { Open, ResponseSubmitted, EvidenceSubmitted, Closed }
public enum SupplierAuditChecklistStatus { Pending, Conform, Nonconform, NotApplicable }
public enum SupplierQualificationStatus { Active, Conditional, Suspended, RequalificationRequired, Approved, Rejected }

public sealed class SupplierAuditChecklistItem
{
    private SupplierAuditChecklistItem() { }
    public Guid Id { get; private set; }
    public Guid SupplierAuditId { get; private set; }
    public int Order { get; private set; }
    public string Category { get; private set; } = "";
    public string Question { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public SupplierAuditChecklistStatus Status { get; private set; }
    public string? Evidence { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset? AnsweredAtUtc { get; private set; }

    internal static SupplierAuditChecklistItem Create(Guid auditId, int order, string category, string question, string reference)
    {
        Text(category, nameof(category), 120); Text(question, nameof(question), 1200); Text(reference, nameof(reference), 400);
        return new() { Id = Guid.CreateVersion7(), SupplierAuditId = auditId, Order = order, Category = category.Trim(), Question = question.Trim(), Reference = reference.Trim(), Status = SupplierAuditChecklistStatus.Pending };
    }

    internal void Answer(SupplierAuditChecklistStatus status, string evidence, string note, DateTimeOffset now)
    {
        if (status == SupplierAuditChecklistStatus.Pending) throw new ArgumentException("Soru sonucu seçilmelidir.");
        if (status == SupplierAuditChecklistStatus.Nonconform) Text(evidence, nameof(evidence), 4000);
        Status = status; Evidence = string.IsNullOrWhiteSpace(evidence) ? null : evidence.Trim(); Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(); AnsweredAtUtc = now;
    }

    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}

public sealed class SupplierAuditFinding
{
    private SupplierAuditFinding() { }
    public Guid Id { get; private set; }
    public Guid SupplierAuditId { get; private set; }
    public string Number { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string RequirementReference { get; private set; } = "";
    public SupplierAuditFindingClassification Classification { get; private set; }
    public bool CapaRequired { get; private set; }
    public Guid? LinkedCapaId { get; private set; }
    public string Owner { get; private set; } = "";
    public Guid OwnerUserId { get; private set; }
    public DateTimeOffset ResponseDueAtUtc { get; private set; }
    public string? SupplierResponse { get; private set; }
    public string? Commitment { get; private set; }
    public DateTimeOffset? CommitmentDueAtUtc { get; private set; }
    public string? Evidence { get; private set; }
    public string? VerificationNote { get; private set; }
    public SupplierAuditFindingStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public static SupplierAuditFinding Create(Guid auditId, string number, string title, string description, string reference, SupplierAuditFindingClassification classification, bool capaRequired, Guid? capaId, Guid ownerUserId, string owner, DateTimeOffset responseDue, DateTimeOffset now)
    {
        Text(number, nameof(number), 80); Text(title, nameof(title), 240); Text(description, nameof(description), 6000); Text(reference, nameof(reference), 500); Text(owner, nameof(owner), 200); if (ownerUserId == Guid.Empty) throw new ArgumentException("Bulgu sorumlusu zorunludur.");
        if (responseDue <= now) throw new ArgumentException("Tedarikçi cevap hedefi gelecekte olmalıdır.");
        if (classification is SupplierAuditFindingClassification.Critical or SupplierAuditFindingClassification.Major) capaRequired = true;
        if (capaRequired && !capaId.HasValue) throw new InvalidOperationException("DÖF gerekli tedarikçi bulgusu ilişkili M.02 kaydı olmadan oluşturulamaz.");
        return new() { Id = Guid.CreateVersion7(), SupplierAuditId = auditId, Number = number.Trim(), Title = title.Trim(), Description = description.Trim(), RequirementReference = reference.Trim(), Classification = classification, CapaRequired = capaRequired, LinkedCapaId = capaId, OwnerUserId = ownerUserId, Owner = owner.Trim(), ResponseDueAtUtc = responseDue, Status = SupplierAuditFindingStatus.Open, CreatedAtUtc = now };
    }

    internal void Respond(string response, string commitment, DateTimeOffset due, DateTimeOffset now)
    {
        Text(response, nameof(response), 8000); Text(commitment, nameof(commitment), 5000); if (due <= now) throw new ArgumentException("Taahhüt hedef tarihi gelecekte olmalıdır.");
        SupplierResponse = response.Trim(); Commitment = commitment.Trim(); CommitmentDueAtUtc = due; Status = SupplierAuditFindingStatus.ResponseSubmitted;
    }

    internal void SubmitEvidence(string evidence, DateTimeOffset now)
    {
        Text(evidence, nameof(evidence), 5000); if (Status != SupplierAuditFindingStatus.ResponseSubmitted) throw new InvalidOperationException("Tedarikçi cevabı olmadan kanıt sunulamaz."); Evidence = evidence.Trim(); Status = SupplierAuditFindingStatus.EvidenceSubmitted;
    }

    internal void Close(string note, bool linkedCapaClosed, DateTimeOffset now)
    {
        Text(note, nameof(note), 3000); if (Status != SupplierAuditFindingStatus.EvidenceSubmitted) throw new InvalidOperationException("Kanıtı sunulmayan bulgu kapatılamaz."); if (CapaRequired && !linkedCapaClosed) throw new InvalidOperationException("İlişkili DÖF kapanmadan tedarikçi bulgusu kapatılamaz."); VerificationNote = note.Trim(); Status = SupplierAuditFindingStatus.Closed; ClosedAtUtc = now;
    }

    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}

public sealed class SupplierAuditInvitation
{
    private SupplierAuditInvitation() { }
    public Guid Id { get; private set; }
    public Guid SupplierAuditId { get; private set; }
    public string RecipientEmail { get; private set; } = "";
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    internal static SupplierAuditInvitation Create(Guid auditId, string email, string token, DateTimeOffset expires, DateTimeOffset now)
    {
        Text(email, nameof(email), 320); if (expires <= now || expires > now.AddDays(30)) throw new ArgumentException("Davet süresi 30 günü aşmadan gelecekte olmalıdır.");
        return new() { Id = Guid.CreateVersion7(), SupplierAuditId = auditId, RecipientEmail = email.Trim().ToLowerInvariant(), TokenHash = Hash(token), ExpiresAtUtc = expires, CreatedAtUtc = now };
    }

    public bool Matches(string token) => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(TokenHash), Convert.FromHexString(Hash(token)));
    public void Use(DateTimeOffset now) { if (UsedAtUtc.HasValue) throw new InvalidOperationException("Tek kullanımlık davet daha önce kullanılmıştır."); if (ExpiresAtUtc <= now) throw new InvalidOperationException("Tedarikçi davetinin süresi dolmuştur."); UsedAtUtc = now; }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}

public sealed class SupplierAudit
{
    private readonly List<SupplierAuditChecklistItem> _checklist = [];
    private readonly List<SupplierAuditFinding> _findings = [];
    private readonly List<SupplierAuditInvitation> _invitations = [];
    private SupplierAudit() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid? SupplierEvaluationId { get; private set; }
    public string SupplierCode { get; private set; } = "";
    public string SupplierName { get; private set; } = "";
    public string SupplierScope { get; private set; } = "";
    public string MaterialOrService { get; private set; } = "";
    public string Country { get; private set; } = "";
    public string Criticality { get; private set; } = "";
    public decimal PastPerformanceScore { get; private set; }
    public int OpenFindingSnapshot { get; private set; }
    public int RiskScore { get; private set; }
    public string RiskBand { get; private set; } = "";
    public int RecommendedFrequencyMonths { get; private set; }
    public string Scope { get; private set; } = "";
    public string Site { get; private set; } = "";
    public Guid LeadAuditorUserId { get; private set; }
    public string LeadAuditor { get; private set; } = "";
    public string LeadAuditorDepartment { get; private set; } = "";
    public string PurchasingOwner { get; private set; } = "";
    public Guid PurchasingOwnerUserId { get; private set; }
    public Guid VerifierUserId { get; private set; }
    public string Verifier { get; private set; } = "";
    public Guid QualityApproverUserId { get; private set; }
    public string QualityApprover { get; private set; } = "";
    public DateTimeOffset PlannedStartUtc { get; private set; }
    public DateTimeOffset PlannedEndUtc { get; private set; }
    public string ChecklistVersion { get; private set; } = "";
    public DateTimeOffset? ChecklistLockedAtUtc { get; private set; }
    public SupplierQualificationStatus QualificationStatus { get; private set; }
    public string? ResultRationale { get; private set; }
    public DateTimeOffset? QualificationValidUntilUtc { get; private set; }
    public bool RequalificationRequired { get; private set; }
    public SupplierAuditStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<SupplierAuditChecklistItem> Checklist => _checklist;
    public IReadOnlyCollection<SupplierAuditFinding> Findings => _findings;
    public IReadOnlyCollection<SupplierAuditInvitation> Invitations => _invitations;

    public static SupplierAudit Create(Guid qualityRecordId, Guid? supplierEvaluationId, string supplierCode, string supplierName, string supplierScope, string materialOrService, string country, string criticality, decimal performance, int openFindings, string scope, string site, Guid auditorId, string auditor, string auditorDepartment, Guid purchasingOwnerUserId, string purchasingOwner, Guid verifierUserId, string verifier, Guid qualityApproverUserId, string qualityApprover, DateTimeOffset start, DateTimeOffset end, string checklistVersion, IEnumerable<(string Category, string Question, string Reference)> questions, DateTimeOffset now)
    {
        if (qualityRecordId == Guid.Empty || auditorId == Guid.Empty || purchasingOwnerUserId == Guid.Empty || verifierUserId == Guid.Empty || qualityApproverUserId == Guid.Empty) throw new ArgumentException("Kalite kaydı ve kayıt görevlerinin kullanıcıları zorunludur.");
        foreach (var (v, n, m) in new[] { (supplierCode, nameof(supplierCode), 80), (supplierName, nameof(supplierName), 240), (supplierScope, nameof(supplierScope), 500), (materialOrService, nameof(materialOrService), 300), (country, nameof(country), 100), (criticality, nameof(criticality), 40), (scope, nameof(scope), 4000), (site, nameof(site), 240), (auditor, nameof(auditor), 200), (auditorDepartment, nameof(auditorDepartment), 160), (purchasingOwner, nameof(purchasingOwner), 200), (verifier, nameof(verifier), 200), (qualityApprover, nameof(qualityApprover), 200), (checklistVersion, nameof(checklistVersion), 80) }) Text(v, n, m);
        if (performance is < 0 or > 100 || openFindings < 0) throw new ArgumentException("Geçmiş performans 0–100 ve açık bulgu sayısı pozitif olmalıdır.");
        if (end <= start) throw new ArgumentException("Denetim bitişi başlangıçtan sonra olmalıdır.");
        var items = questions.Where(x => !string.IsNullOrWhiteSpace(x.Question)).ToArray(); if (items.Length == 0) throw new ArgumentException("En az bir soru listesi maddesi gerekir.");
        var criticalityPoints = criticality.Trim().ToLowerInvariant() switch { "kritik" => 50, "yüksek" => 40, "orta" => 25, "düşük" => 10, _ => throw new ArgumentException("Kritiklik Kritik, Yüksek, Orta veya Düşük olmalıdır.") };
        var risk = Math.Min(100, criticalityPoints + (int)Math.Round((100 - performance) * .35m) + Math.Min(20, openFindings * 4));
        var band = risk >= 75 ? "Kritik" : risk >= 55 ? "Yüksek" : risk >= 30 ? "Orta" : "Düşük";
        var frequency = risk >= 75 ? 12 : risk >= 55 ? 18 : risk >= 30 ? 24 : 36;
        var audit = new SupplierAudit { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, SupplierEvaluationId = supplierEvaluationId, SupplierCode = supplierCode.Trim(), SupplierName = supplierName.Trim(), SupplierScope = supplierScope.Trim(), MaterialOrService = materialOrService.Trim(), Country = country.Trim(), Criticality = criticality.Trim(), PastPerformanceScore = performance, OpenFindingSnapshot = openFindings, RiskScore = risk, RiskBand = band, RecommendedFrequencyMonths = frequency, Scope = scope.Trim(), Site = site.Trim(), LeadAuditorUserId = auditorId, LeadAuditor = auditor.Trim(), LeadAuditorDepartment = auditorDepartment.Trim(), PurchasingOwnerUserId = purchasingOwnerUserId, PurchasingOwner = purchasingOwner.Trim(), VerifierUserId = verifierUserId, Verifier = verifier.Trim(), QualityApproverUserId = qualityApproverUserId, QualityApprover = qualityApprover.Trim(), PlannedStartUtc = start, PlannedEndUtc = end, ChecklistVersion = checklistVersion.Trim(), QualificationStatus = SupplierQualificationStatus.Active, Status = SupplierAuditStatus.RiskPlan, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
        var order = 1; foreach (var item in items) audit._checklist.Add(SupplierAuditChecklistItem.Create(audit.Id, order++, item.Category, item.Question, item.Reference)); return audit;
    }

    public void Transition(long version, string code, DateTimeOffset now)
    {
        EnsureVersion(version);
        switch (code.Trim().ToLowerInvariant())
        {
            case "define-scope": Ensure(SupplierAuditStatus.RiskPlan); Move(SupplierAuditStatus.ScopeChecklist, now); break;
            case "assign-auditor": Ensure(SupplierAuditStatus.ScopeChecklist); Move(SupplierAuditStatus.AuditorAssignment, now); break;
            case "start-audit": Ensure(SupplierAuditStatus.AuditorAssignment); ChecklistLockedAtUtc = now; Move(SupplierAuditStatus.Execution, now); break;
            case "complete-audit": Ensure(SupplierAuditStatus.Execution); if (_checklist.Any(x => x.Status == SupplierAuditChecklistStatus.Pending)) throw new InvalidOperationException("Tüm soru listesi maddeleri cevaplanmadan uygulama tamamlanamaz."); Move(SupplierAuditStatus.Findings, now); break;
            case "request-supplier-response": Ensure(SupplierAuditStatus.Findings); Move(SupplierAuditStatus.SupplierResponse, now); break;
            case "start-verification": Ensure(SupplierAuditStatus.SupplierResponse); if (_findings.Any(x => x.Status == SupplierAuditFindingStatus.Open)) throw new InvalidOperationException("Tüm bulguların tedarikçi cevabı olmadan kanıt doğrulamaya geçilemez."); Move(SupplierAuditStatus.EvidenceVerification, now); break;
            case "start-capa": Ensure(SupplierAuditStatus.EvidenceVerification); if (_findings.Any(x => x.Status is SupplierAuditFindingStatus.Open or SupplierAuditFindingStatus.ResponseSubmitted)) throw new InvalidOperationException("Tüm bulguların kanıtı sunulmadan DÖF/CAPA aşamasına geçilemez."); Move(SupplierAuditStatus.Capa, now); break;
            case "record-result": Ensure(SupplierAuditStatus.Capa); if (_findings.Any(x => x.Status != SupplierAuditFindingStatus.Closed)) throw new InvalidOperationException("Tüm bulgular ve bağlı DÖF kayıtları kapanmadan denetim sonucu oluşturulamaz."); Move(SupplierAuditStatus.AuditResult, now); break;
            case "close": Ensure(SupplierAuditStatus.AuditResult); if (string.IsNullOrWhiteSpace(ResultRationale)) throw new InvalidOperationException("Tedarikçi nitelendirme kararı olmadan denetim kapatılamaz."); ClosedAtUtc = now; Move(SupplierAuditStatus.Closed, now); break;
            default: throw new ArgumentException("Bilinmeyen tedarikçi denetimi geçişi.");
        }
    }

    public void Answer(long version, Guid itemId, SupplierAuditChecklistStatus status, string evidence, string note, DateTimeOffset now) { EnsureVersion(version); Ensure(SupplierAuditStatus.Execution); (_checklist.SingleOrDefault(x => x.Id == itemId) ?? throw new ArgumentException("Soru bulunamadı.")).Answer(status, evidence, note, now); Touch(now); }
    public void AddFinding(long version, SupplierAuditFinding finding, DateTimeOffset now) { EnsureVersion(version); if (Status is not (SupplierAuditStatus.Execution or SupplierAuditStatus.Findings)) throw new InvalidOperationException("Bulgu yalnız uygulama veya bulgular aşamasında açılabilir."); _findings.Add(finding); if (finding.Classification == SupplierAuditFindingClassification.Critical) { QualificationStatus = SupplierQualificationStatus.Suspended; RequalificationRequired = true; } else if (finding.Classification == SupplierAuditFindingClassification.Major && QualificationStatus == SupplierQualificationStatus.Active) { QualificationStatus = SupplierQualificationStatus.RequalificationRequired; RequalificationRequired = true; } Touch(now); }
    public void RespondFinding(long version, Guid findingId, string response, string commitment, DateTimeOffset due, DateTimeOffset now) { EnsureVersion(version); Ensure(SupplierAuditStatus.SupplierResponse); (_findings.SingleOrDefault(x => x.Id == findingId) ?? throw new ArgumentException("Bulgu bulunamadı.")).Respond(response, commitment, due, now); Touch(now); }
    public void SubmitFindingEvidence(long version, Guid findingId, string evidence, DateTimeOffset now) { EnsureVersion(version); if (Status is not (SupplierAuditStatus.SupplierResponse or SupplierAuditStatus.EvidenceVerification)) throw new InvalidOperationException("Kanıt bu aşamada sunulamaz."); (_findings.SingleOrDefault(x => x.Id == findingId) ?? throw new ArgumentException("Bulgu bulunamadı.")).SubmitEvidence(evidence, now); Touch(now); }
    public void CloseFinding(long version, Guid findingId, string note, bool capaClosed, DateTimeOffset now) { EnsureVersion(version); Ensure(SupplierAuditStatus.Capa); (_findings.SingleOrDefault(x => x.Id == findingId) ?? throw new ArgumentException("Bulgu bulunamadı.")).Close(note, capaClosed, now); Touch(now); }
    public SupplierAuditInvitation CreateInvitation(long version, string email, string token, DateTimeOffset expires, DateTimeOffset now) { EnsureVersion(version); Ensure(SupplierAuditStatus.SupplierResponse); var invitation = SupplierAuditInvitation.Create(Id, email, token, expires, now); _invitations.Add(invitation); Touch(now); return invitation; }
    public void RecordResult(long version, SupplierQualificationStatus decision, string rationale, DateTimeOffset? validUntil, bool requalification, DateTimeOffset now) { EnsureVersion(version); Ensure(SupplierAuditStatus.AuditResult); Text(rationale, nameof(rationale), 4000); if (decision == SupplierQualificationStatus.Active) throw new ArgumentException("Nihai karar Onaylı, Koşullu, Askıda veya Reddedildi olmalıdır."); if (decision == SupplierQualificationStatus.Approved && _findings.Any(x => x.Classification == SupplierAuditFindingClassification.Critical)) throw new InvalidOperationException("Kritik bulgulu tedarikçi doğrudan onaylanamaz; yeniden nitelendirme gerekir."); QualificationStatus = decision; ResultRationale = rationale.Trim(); QualificationValidUntilUtc = validUntil; RequalificationRequired = requalification || decision is SupplierQualificationStatus.Suspended or SupplierQualificationStatus.Rejected or SupplierQualificationStatus.RequalificationRequired; Touch(now); }
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("Tedarikçi denetimi başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void Ensure(SupplierAuditStatus status) { if (Status != status) throw new InvalidOperationException($"Geçersiz tedarikçi denetimi aşaması. Beklenen: {status}, mevcut: {Status}."); }
    private void Move(SupplierAuditStatus status, DateTimeOffset now) { Status = status; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
