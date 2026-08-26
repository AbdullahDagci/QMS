namespace Qms.Domain.Complaints;

public sealed class Complaint
{
    private readonly List<ComplaintInvestigation> _investigations = [];
    private readonly List<ComplaintResponse> _responses = [];
    private Complaint() { }
    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string Product { get; private set; } = string.Empty;
    public string? BatchNumber { get; private set; }
    public DateTimeOffset EventAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public string ComplaintType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ComplaintSeverity Severity { get; private set; }
    public bool HasHealthImpact { get; private set; }
    public bool SuspectedAdverseEvent { get; private set; }
    public bool SampleExpected { get; private set; }
    public bool ReturnExpected { get; private set; }
    public string AttachmentSummary { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public DateTimeOffset PreliminaryResponseDueAtUtc { get; private set; }
    public DateTimeOffset FinalResponseDueAtUtc { get; private set; }
    public int SimilarComplaintCount { get; private set; }
    public bool TrendFlagged { get; private set; }
    public Guid? LinkedDeviationId { get; private set; }
    public Guid? LinkedCapaId { get; private set; }
    public Guid? PharmacovigilanceRecordId { get; private set; }
    public string? PharmacovigilanceStatus { get; private set; }
    public string? ImpactAssessment { get; private set; }
    public string? ConfirmedRootCause { get; private set; }
    public bool? CapaRequired { get; private set; }
    public string? ClosureNote { get; private set; }
    public ComplaintStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<ComplaintInvestigation> Investigations => _investigations;
    public IReadOnlyCollection<ComplaintResponse> Responses => _responses;

    public static Complaint Create(Guid qualityRecordId, string channel, string customerName, string country, string product, string? batchNumber, DateTimeOffset eventAt, DateTimeOffset receivedAt, string complaintType, string description, ComplaintSeverity severity, bool healthImpact, bool adverseEvent, bool sampleExpected, bool returnExpected, string attachmentSummary, Guid ownerUserId, string owner, DateTimeOffset preliminaryDue, DateTimeOffset finalDue, int similarCount, IEnumerable<(Guid DepartmentId, string Department, Guid InvestigatorUserId, string Investigator)> investigations, DateTimeOffset now)
    {
        if (qualityRecordId == Guid.Empty) throw new ArgumentException("Kalite kaydı zorunludur.");
        Text(channel, nameof(channel), 80); Text(customerName, nameof(customerName), 200); Text(country, nameof(country), 100); Text(product, nameof(product), 200); Text(complaintType, nameof(complaintType), 120); Text(description, nameof(description), 6000); Text(owner, nameof(owner), 200); if (ownerUserId == Guid.Empty) throw new ArgumentException("Şikâyet sahibi zorunludur.");
        if (eventAt > receivedAt) throw new ArgumentException("Olay tarihi şikâyetin alınma tarihinden sonra olamaz.");
        if (receivedAt > now.AddMinutes(1)) throw new ArgumentException("Alınma tarihi gelecekte olamaz.");
        if (preliminaryDue <= receivedAt || finalDue <= preliminaryDue) throw new ArgumentException("Yanıt hedef tarihleri kronolojik ve gelecekte olmalıdır.");
        var unique = investigations.GroupBy(x => x.DepartmentId).Select(x => x.First()).ToArray();
        if (unique.Length == 0) throw new ArgumentException("En az bir araştırma bölümü seçilmelidir.");
        var entity = new Complaint { Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, Channel = channel.Trim(), CustomerName = customerName.Trim(), Country = country.Trim(), Product = product.Trim(), BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber.Trim(), EventAtUtc = eventAt, ReceivedAtUtc = receivedAt, ComplaintType = complaintType.Trim(), Description = description.Trim(), Severity = severity, HasHealthImpact = healthImpact, SuspectedAdverseEvent = adverseEvent, SampleExpected = sampleExpected, ReturnExpected = returnExpected, AttachmentSummary = attachmentSummary?.Trim() ?? string.Empty, OwnerUserId = ownerUserId, Owner = owner.Trim(), PreliminaryResponseDueAtUtc = preliminaryDue, FinalResponseDueAtUtc = finalDue, SimilarComplaintCount = similarCount, TrendFlagged = similarCount >= 2, Status = ComplaintStatus.Received, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 };
        foreach (var investigation in unique) entity._investigations.Add(ComplaintInvestigation.Create(entity.Id, investigation.DepartmentId, investigation.Department, investigation.InvestigatorUserId, investigation.Investigator));
        return entity;
    }

    public void StartTriage(long version, DateTimeOffset now) { Ensure(version, ComplaintStatus.Received); Move(ComplaintStatus.Triage, now); }
    public void CompleteTriage(long version, Guid? deviationId, Guid? pvRecordId, DateTimeOffset now) { Ensure(version, ComplaintStatus.Triage); LinkedDeviationId = deviationId; PharmacovigilanceRecordId = pvRecordId; PharmacovigilanceStatus = pvRecordId.HasValue ? "Transferred" : null; Move(ComplaintStatus.PreliminaryResponse, now); }
    public ComplaintResponse AddResponse(long version, ComplaintResponseType type, string content, Guid userId, string user, DateTimeOffset now)
    {
        EnsureVersion(version);
        if (type == ComplaintResponseType.Preliminary && Status != ComplaintStatus.PreliminaryResponse) throw new InvalidOperationException("Ön yanıt yalnız ön yanıt aşamasında hazırlanabilir.");
        if (type == ComplaintResponseType.Final && Status != ComplaintStatus.FinalResponseApproval) throw new InvalidOperationException("Nihai yanıt yalnız nihai yanıt onayı aşamasında hazırlanabilir.");
        var next = _responses.Where(x => x.ResponseType == type).Select(x => x.VersionNumber).DefaultIfEmpty(0).Max() + 1;
        var response = ComplaintResponse.Create(Id, type, next, content, userId, user, now); _responses.Add(response); Touch(now); return response;
    }
    public void ApproveResponse(long version, Guid responseId, Guid userId, string user, DateTimeOffset now) { EnsureVersion(version); var response = _responses.SingleOrDefault(x => x.Id == responseId) ?? throw new ArgumentException("Yanıt sürümü bulunamadı."); response.Approve(userId, user, now); Touch(now); }
    public void StartInvestigation(long version, DateTimeOffset now) { Ensure(version, ComplaintStatus.PreliminaryResponse); if (!_responses.Any(x => x.ResponseType == ComplaintResponseType.Preliminary && x.Status == ComplaintResponseStatus.Approved)) throw new InvalidOperationException("Onaylı ön yanıt olmadan araştırma başlatılamaz."); Move(ComplaintStatus.Investigation, now); }
    public void CompleteInvestigation(long version, Guid investigationId, string findings, string rootCause, DateTimeOffset now) { Ensure(version, ComplaintStatus.Investigation); var item = _investigations.SingleOrDefault(x => x.Id == investigationId) ?? throw new ArgumentException("Araştırma bulunamadı."); item.Complete(findings, rootCause, now); Touch(now); }
    public void FinishInvestigations(long version, DateTimeOffset now) { Ensure(version, ComplaintStatus.Investigation); if (_investigations.Any(x => x.Status != ComplaintInvestigationStatus.Completed)) throw new InvalidOperationException("Tüm paralel araştırmalar tamamlanmadan etki değerlendirmesine geçilemez."); Move(ComplaintStatus.ImpactAssessment, now); }
    public void CompleteImpact(long version, string assessment, string rootCause, DateTimeOffset now) { Ensure(version, ComplaintStatus.ImpactAssessment); Text(assessment, nameof(assessment), 5000); Text(rootCause, nameof(rootCause), 4000); ImpactAssessment = assessment.Trim(); ConfirmedRootCause = rootCause.Trim(); Move(ComplaintStatus.CapaDecision, now); }
    public void DecideCapa(long version, bool required, Guid? capaId, DateTimeOffset now) { Ensure(version, ComplaintStatus.CapaDecision); if (required && !capaId.HasValue) throw new InvalidOperationException("DÖF gerekli kararında ilişkili M.02 kaydı oluşturulmalıdır."); CapaRequired = required; LinkedCapaId = capaId; Move(ComplaintStatus.FinalResponseApproval, now); }
    public void Close(long version, string note, DateTimeOffset now) { Ensure(version, ComplaintStatus.FinalResponseApproval); Text(note, nameof(note), 2000); if (!_responses.Any(x => x.ResponseType == ComplaintResponseType.Final && x.Status == ComplaintResponseStatus.Approved)) throw new InvalidOperationException("Onaylı nihai yanıt olmadan şikâyet kapatılamaz."); if (SuspectedAdverseEvent && PharmacovigilanceStatus != "Transferred") throw new InvalidOperationException("Farmakovijilans aktarımı tamamlanmadan şikâyet kapatılamaz."); ClosureNote = note.Trim(); ClosedAtUtc = now; Move(ComplaintStatus.Closed, now); }
    private void Ensure(long version, ComplaintStatus status) { EnsureVersion(version); if (Status != status) throw new InvalidOperationException($"Geçersiz şikâyet aşaması. Beklenen: {status}, mevcut: {Status}."); }
    private void EnsureVersion(long version) { if (Version != version) throw new InvalidOperationException("Şikâyet başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin."); }
    private void Move(ComplaintStatus status, DateTimeOffset now) { Status = status; Touch(now); }
    private void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
