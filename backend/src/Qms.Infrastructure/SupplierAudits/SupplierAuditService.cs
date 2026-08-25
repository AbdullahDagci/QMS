using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Security;
using Qms.Application.SupplierAudits;
using Qms.Contracts.Common;
using Qms.Contracts.SupplierAudits;
using Qms.Domain.AuditTrail;
using Qms.Domain.Capas;
using Qms.Domain.QualityRecords;
using Qms.Domain.SupplierAudits;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.SupplierAudits;

public sealed class SupplierAuditService(QmsDbContext db, TimeProvider time, ICurrentUser user) : ISupplierAuditService
{
    private static readonly Guid DefaultDepartment = Guid.Parse("01991f70-6f40-7000-8000-000000000002");

    public async Task<PagedResponse<SupplierAuditListItemResponse>> SearchAsync(SupplierAuditSearchRequest r, CancellationToken ct)
    {
        if (r.Page < 1 || r.PageSize is not (10 or 25 or 50 or 100)) throw new ArgumentException("Sayfa bilgisi geçersizdir.");
        var query = from audit in db.SupplierAudits.AsNoTracking() join record in db.QualityRecords.AsNoTracking() on audit.QualityRecordId equals record.Id select new Row { Audit = audit, Number = record.RecordNumber };
        foreach (var filter in r.Filters ?? []) query = Filter(query, filter);
        var total = await query.LongCountAsync(ct); query = Sort(query, r.SortBy, r.SortDirection);
        var items = await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).Select(x => new SupplierAuditListItemResponse(x.Audit.Id, x.Number, x.Audit.SupplierCode, x.Audit.SupplierName, x.Audit.SupplierScope, x.Audit.MaterialOrService, x.Audit.Criticality, x.Audit.RiskScore, x.Audit.RiskBand, x.Audit.RecommendedFrequencyMonths, x.Audit.QualificationStatus.ToString(), x.Audit.PlannedStartUtc, x.Audit.Findings.Count, x.Audit.Findings.Count(f => f.Status != SupplierAuditFindingStatus.Closed), x.Audit.Status.ToString(), x.Audit.CreatedAtUtc, x.Audit.Version)).ToListAsync(ct);
        return new(items, r.Page, r.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)r.PageSize));
    }

    public async Task<SupplierAuditDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var audit = await db.SupplierAudits.AsNoTracking().AsSplitQuery().Include(x => x.Checklist).Include(x => x.Findings).Include(x => x.Invitations).SingleOrDefaultAsync(x => x.Id == id, ct); if (audit is null) return null;
        var number = await db.QualityRecords.Where(x => x.Id == audit.QualityRecordId).Select(x => x.RecordNumber).SingleAsync(ct);
        var capaIds = audit.Findings.Where(x => x.LinkedCapaId.HasValue).Select(x => x.LinkedCapaId!.Value).ToArray();
        var capaNumbers = await (from capa in db.Capas.AsNoTracking() join qr in db.QualityRecords.AsNoTracking() on capa.QualityRecordId equals qr.Id where capaIds.Contains(capa.Id) select new { capa.Id, qr.RecordNumber }).ToDictionaryAsync(x => x.Id, x => x.RecordNumber, ct);
        var events = await db.AuditEvents.AsNoTracking().Where(x => x.AggregateType == "SupplierAudit" && x.AggregateId == id).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct);
        var record = new SupplierAuditRecordResponse(audit.Id, audit.QualityRecordId, audit.SupplierEvaluationId, number, audit.SupplierCode, audit.SupplierName, audit.SupplierScope, audit.MaterialOrService, audit.Country, audit.Criticality, audit.PastPerformanceScore, audit.OpenFindingSnapshot, audit.RiskScore, audit.RiskBand, audit.RecommendedFrequencyMonths, audit.Scope, audit.Site, audit.LeadAuditorUserId, audit.LeadAuditor, audit.LeadAuditorDepartment, audit.PurchasingOwner, audit.PlannedStartUtc, audit.PlannedEndUtc, audit.ChecklistVersion, audit.ChecklistLockedAtUtc, audit.QualificationStatus.ToString(), audit.ResultRationale, audit.QualificationValidUntilUtc, audit.RequalificationRequired, audit.Status.ToString(), audit.CreatedAtUtc, audit.UpdatedAtUtc, audit.ClosedAtUtc, audit.Version);
        return new(record,
            audit.Checklist.OrderBy(x => x.Order).Select(x => new SupplierAuditChecklistResponse(x.Id, x.Order, x.Category, x.Question, x.Reference, x.Status.ToString(), x.Evidence, x.Note, x.AnsweredAtUtc)).ToList(),
            audit.Findings.OrderBy(x => x.Number).Select(x => new SupplierAuditFindingResponse(x.Id, x.Number, x.Title, x.Description, x.RequirementReference, x.Classification.ToString(), x.CapaRequired, x.LinkedCapaId, x.LinkedCapaId is Guid capaId ? capaNumbers.GetValueOrDefault(capaId) : null, x.Owner, x.ResponseDueAtUtc, x.SupplierResponse, x.Commitment, x.CommitmentDueAtUtc, x.Evidence, x.VerificationNote, x.Status.ToString(), x.CreatedAtUtc, x.ClosedAtUtc)).ToList(),
            audit.Invitations.OrderByDescending(x => x.CreatedAtUtc).Select(x => new SupplierAuditInvitationResponse(x.Id, x.RecipientEmail, x.ExpiresAtUtc, x.CreatedAtUtc, x.UsedAtUtc)).ToList(),
            events.Select(x => new SupplierAuditEventResponse(x.Id, x.AggregateVersion, x.EventType, x.ActorDisplayNameSnapshot, x.OccurredAtUtc, x.Reason, x.Payload.RootElement.Clone())).ToList(), Transitions(audit.Status));
    }

    public async Task<SupplierAuditDetailsResponse> CreateAsync(CreateSupplierAuditRequest r, CancellationToken ct)
    {
        EnsurePlan(); var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var number = $"TD-{now.Year}-{await Next("supplier-audit", now.Year, tx, ct):000000}"; var qualityRecord = QualityRecord.Create(number, "supplier-audit", user.Id, user.DepartmentId ?? DefaultDepartment, now);
        var audit = SupplierAudit.Create(qualityRecord.Id, r.SupplierEvaluationId, r.SupplierCode, r.SupplierName, r.SupplierScope, r.MaterialOrService, r.Country, r.Criticality, r.PastPerformanceScore, r.OpenFindingCount, r.Scope, r.Site, r.LeadAuditorUserId, r.LeadAuditor, r.LeadAuditorDepartment, r.PurchasingOwner, r.PlannedStartUtc.ToUniversalTime(), r.PlannedEndUtc.ToUniversalTime(), r.ChecklistVersion, r.Checklist.Select(x => (x.Category, x.Question, x.Reference)), now);
        db.QualityRecords.Add(qualityRecord); db.SupplierAudits.Add(audit);
        db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("SupplierAudit", audit.Id, WorkflowTaskRoles.SupplierAuditPlanner, user.Id, user.DepartmentId, now, audit.PlannedStartUtc));
        db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("SupplierAudit", audit.Id, WorkflowTaskRoles.SupplierAuditLeadAuditor, audit.LeadAuditorUserId, user.DepartmentId, now, audit.PlannedEndUtc));
        db.AuditEvents.Add(Audit(audit, "SupplierAuditCreated", now, new { number, audit.SupplierCode, audit.SupplierName, audit.RiskScore, audit.RiskBand, audit.RecommendedFrequencyMonths }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (await GetDetailsAsync(audit.Id, ct))!;
    }

    public async Task<SupplierAuditDetailsResponse?> TransitionAsync(Guid id, TransitionSupplierAuditRequest r, CancellationToken ct)
    {
        var audit = await Load(id, ct); if (audit is null) return null; EnsureTransition(audit.Status); var now = time.GetUtcNow(); var from = audit.Status; await using var tx = await db.Database.BeginTransactionAsync(ct); audit.Transition(r.ExpectedVersion, r.Transition, now); db.AuditEvents.Add(Audit(audit, "SupplierAuditStatusChanged", now, new { from = from.ToString(), to = audit.Status.ToString(), transition = r.Transition })); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public Task<SupplierAuditDetailsResponse?> AnswerChecklistAsync(Guid id, Guid itemId, AnswerSupplierAuditChecklistRequest r, CancellationToken ct) => Mutate(id, ct, (audit, now) => { EnsureExecute(); if (!Enum.TryParse<SupplierAuditChecklistStatus>(r.Status, true, out var status)) throw new ArgumentException("Soru sonucu geçersizdir."); audit.Answer(r.ExpectedVersion, itemId, status, r.Evidence, r.Note, now); return ("SupplierAuditChecklistAnswered", (object)new { itemId, status }, r.Note); });

    public async Task<SupplierAuditDetailsResponse?> AddFindingAsync(Guid id, AddSupplierAuditFindingRequest r, CancellationToken ct)
    {
        EnsureExecute(); if (!Enum.TryParse<SupplierAuditFindingClassification>(r.Classification, true, out var classification)) throw new ArgumentException("Bulgu sınıfı geçersizdir."); var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var needsCapa = r.CapaRequired || classification is SupplierAuditFindingClassification.Critical or SupplierAuditFindingClassification.Major; Guid? capaId = null;
        if (needsCapa)
        {
            var capaNumber = $"DÖF-{now.Year}-{await Next("capa", now.Year, tx, ct):000000}"; var qualityRecord = QualityRecord.Create(capaNumber, "capa", user.Id, user.DepartmentId ?? DefaultDepartment, now);
            var capa = Capa.Create(qualityRecord.Id, null, "SupplierAudit", r.Title, r.Description, $"Tedarikçi denetim bulgusu: {r.RequirementReference}", "Tedarikçi kaynağında düzeltme ve tekrar önleme", r.Owner, r.ResponseDueAtUtc.ToUniversalTime(), true, "Tedarikçi kanıtı ve kalite doğrulaması", audit.SupplierScope, 30, "Aynı tedarikçi uygunsuzluğunun tekrar etmemesi", "Tedarikçi Kalite", now);
            db.QualityRecords.Add(qualityRecord); db.Capas.Add(capa); db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Capa", capa.Id, WorkflowTaskRoles.Initiator, user.Id, user.DepartmentId, now, capa.TargetDateUtc)); capaId = capa.Id;
            db.AuditEvents.Add(AuditEvent.Create("Capa", capa.Id, capa.Version, "CapaCreatedFromSupplierAuditFinding", user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { supplierAuditId = audit.Id })));
        }
        var findingNumber = $"TDB-{now.Year}-{await Next("supplier-audit-finding", now.Year, tx, ct):000000}"; var finding = SupplierAuditFinding.Create(audit.Id, findingNumber, r.Title, r.Description, r.RequirementReference, classification, needsCapa, capaId, r.Owner, r.ResponseDueAtUtc.ToUniversalTime(), now); var previousQualification = audit.QualificationStatus; audit.AddFinding(r.ExpectedVersion, finding, now); db.SupplierAuditFindings.Add(finding); db.AuditEvents.Add(Audit(audit, "SupplierAuditFindingCreated", now, new { finding.Number, finding.Classification, capaId, previousQualification, audit.QualificationStatus })); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public Task<SupplierAuditDetailsResponse?> RespondFindingAsync(Guid id, Guid findingId, RespondSupplierAuditFindingRequest r, CancellationToken ct) => Mutate(id, ct, (audit, now) => { EnsureRespond(); audit.RespondFinding(r.ExpectedVersion, findingId, r.SupplierResponse, r.Commitment, r.CommitmentDueAtUtc.ToUniversalTime(), now); return ("SupplierAuditFindingResponseSubmitted", (object)new { findingId, r.CommitmentDueAtUtc }, r.SupplierResponse); });
    public Task<SupplierAuditDetailsResponse?> SubmitEvidenceAsync(Guid id, Guid findingId, SubmitSupplierAuditEvidenceRequest r, CancellationToken ct) => Mutate(id, ct, (audit, now) => { EnsureRespond(); audit.SubmitFindingEvidence(r.ExpectedVersion, findingId, r.Evidence, now); return ("SupplierAuditFindingEvidenceSubmitted", (object)new { findingId }, r.Evidence); });

    public async Task<SupplierAuditDetailsResponse?> CloseFindingAsync(Guid id, Guid findingId, CloseSupplierAuditFindingRequest r, CancellationToken ct)
    {
        EnsureApprove(); var audit = await Load(id, ct); if (audit is null) return null; var finding = audit.Findings.SingleOrDefault(x => x.Id == findingId) ?? throw new ArgumentException("Bulgu bulunamadı."); var capaClosed = !finding.CapaRequired || finding.LinkedCapaId is Guid capaId && await db.Capas.AnyAsync(x => x.Id == capaId && x.Status == CapaStatus.Closed, ct); var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); audit.CloseFinding(r.ExpectedVersion, findingId, r.VerificationNote, capaClosed, now); db.AuditEvents.Add(Audit(audit, "SupplierAuditFindingClosed", now, new { findingId, finding.LinkedCapaId }, r.VerificationNote)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<SupplierInvitationCreatedResponse?> CreateInvitationAsync(Guid id, CreateSupplierInvitationRequest r, CancellationToken ct)
    {
        EnsureRespond(); var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); await using var tx = await db.Database.BeginTransactionAsync(ct); var invitation = audit.CreateInvitation(r.ExpectedVersion, r.RecipientEmail, token, r.ExpiresAtUtc.ToUniversalTime(), now); db.SupplierAuditInvitations.Add(invitation); db.AuditEvents.Add(Audit(audit, "SupplierAuditInvitationCreated", now, new { invitation.RecipientEmail, invitation.ExpiresAtUtc })); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new((await GetDetailsAsync(id, ct))!, token, $"/supplier-response?token={token}");
    }

    public async Task<SupplierInvitationReceiptResponse> SubmitInvitationResponseAsync(SupplierInvitationSubmissionRequest r, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Token))); var invitation = await db.SupplierAuditInvitations.SingleOrDefaultAsync(x => x.TokenHash == hash, ct) ?? throw new ArgumentException("Tedarikçi daveti geçersizdir."); var audit = await Load(invitation.SupplierAuditId, ct) ?? throw new ArgumentException("Denetim bulunamadı."); var finding = audit.Findings.SingleOrDefault(x => x.Id == r.FindingId) ?? throw new ArgumentException("Bulgu bulunamadı."); var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); invitation.Use(now); audit.RespondFinding(audit.Version, finding.Id, r.SupplierResponse, r.Commitment, r.CommitmentDueAtUtc.ToUniversalTime(), now); audit.SubmitFindingEvidence(audit.Version, finding.Id, r.Evidence, now); db.AuditEvents.Add(AuditEvent.Create("SupplierAudit", audit.Id, audit.Version, "SupplierAuditInvitationResponseAccepted", Guid.Empty, "Tedarikçi güvenli yanıt ekranı", now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { findingId = finding.Id, invitation.RecipientEmail }), r.SupplierResponse)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); var number = await db.QualityRecords.Where(x => x.Id == audit.QualityRecordId).Select(x => x.RecordNumber).SingleAsync(ct); return new(number, finding.Number, now);
    }

    public Task<SupplierAuditDetailsResponse?> RecordResultAsync(Guid id, RecordSupplierAuditResultRequest r, CancellationToken ct) => Mutate(id, ct, (audit, now) => { EnsureApprove(); if (!Enum.TryParse<SupplierQualificationStatus>(r.Decision, true, out var decision)) throw new ArgumentException("Tedarikçi nitelendirme kararı geçersizdir."); audit.RecordResult(r.ExpectedVersion, decision, r.Rationale, r.ValidUntilUtc?.ToUniversalTime(), r.RequalificationRequired, now); return ("SupplierAuditResultRecorded", (object)new { decision, r.ValidUntilUtc, r.RequalificationRequired }, r.Rationale); });

    private async Task<SupplierAuditDetailsResponse?> Mutate(Guid id, CancellationToken ct, Func<SupplierAudit, DateTimeOffset, (string Type, object Payload, string? Reason)> action) { var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); var result = action(audit, now); db.AuditEvents.Add(Audit(audit, result.Type, now, result.Payload, result.Reason)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct); }
    private Task<SupplierAudit?> Load(Guid id, CancellationToken ct) => db.SupplierAudits.AsSplitQuery().Include(x => x.Checklist).Include(x => x.Findings).Include(x => x.Invitations).SingleOrDefaultAsync(x => x.Id == id, ct);
    private void EnsurePlan() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.DepartmentManager)) return; throw new QmsForbiddenException("Tedarikçi denetimi planlamak için KG veya bölüm yöneticisi rolü gerekir."); }
    private void EnsureExecute() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.Investigator) || user.IsInRole(QmsRoles.Approver)) return; throw new QmsForbiddenException("Tedarikçi denetimini uygulamak için denetçi yetkisi gerekir."); }
    private void EnsureRespond() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.DepartmentManager) || user.IsInRole(QmsRoles.ActionOwner)) return; throw new QmsForbiddenException("Tedarikçi cevabı ve kanıtı için atanmış yanıt yetkisi gerekir."); }
    private void EnsureApprove() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.Approver) || user.IsInRole(QmsRoles.QualifiedPerson)) return; throw new QmsForbiddenException("Tedarikçi bulgu ve sonuç doğrulaması için onaylayan rolü gerekir."); }
    private void EnsureTransition(SupplierAuditStatus status) { if (status is SupplierAuditStatus.RiskPlan or SupplierAuditStatus.ScopeChecklist) EnsurePlan(); else if (status is SupplierAuditStatus.AuditorAssignment or SupplierAuditStatus.Execution or SupplierAuditStatus.Findings) EnsureExecute(); else if (status == SupplierAuditStatus.SupplierResponse) EnsureRespond(); else EnsureApprove(); }
    private AuditEvent Audit(SupplierAudit audit, string type, DateTimeOffset now, object payload, string? reason = null) => AuditEvent.Create("SupplierAudit", audit.Id, audit.Version, type, user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(payload), reason);
    private static IReadOnlyList<SupplierAuditTransitionResponse> Transitions(SupplierAuditStatus status) => status switch { SupplierAuditStatus.RiskPlan => [new("define-scope", "Kapsam ve soru listesini sabitle")], SupplierAuditStatus.ScopeChecklist => [new("assign-auditor", "Denetçiyi ata")], SupplierAuditStatus.AuditorAssignment => [new("start-audit", "Tedarikçi denetimini başlat")], SupplierAuditStatus.Execution => [new("complete-audit", "Uygulamayı ve bulgu girişini tamamla")], SupplierAuditStatus.Findings => [new("request-supplier-response", "Tedarikçi cevabını aç")], SupplierAuditStatus.SupplierResponse => [new("start-verification", "Kanıt doğrulamaya geç")], SupplierAuditStatus.EvidenceVerification => [new("start-capa", "DÖF/CAPA doğrulamasına geç")], SupplierAuditStatus.Capa => [new("record-result", "Denetim sonucuna geç")], SupplierAuditStatus.AuditResult => [new("close", "Tedarikçi denetimini kapat")], _ => [] };
    private static IQueryable<Row> Filter(IQueryable<Row> q, ColumnFilterRequest f) { var key = f.Field.Trim().ToLowerInvariant(); var value = f.Value?.Trim() ?? ""; return key switch { "recordnumber" => q.Where(x => x.Number.ToLower().Contains(value.ToLower())), "suppliercode" => q.Where(x => x.Audit.SupplierCode.ToLower().Contains(value.ToLower())), "suppliername" => q.Where(x => x.Audit.SupplierName.ToLower().Contains(value.ToLower())), "supplierscope" => q.Where(x => x.Audit.SupplierScope.ToLower().Contains(value.ToLower())), "materialorservice" => q.Where(x => x.Audit.MaterialOrService.ToLower().Contains(value.ToLower())), "criticality" => q.Where(x => x.Audit.Criticality.ToLower() == value.ToLower()), "riskband" => q.Where(x => x.Audit.RiskBand.ToLower() == value.ToLower()), "qualificationstatus" => Enum.TryParse<SupplierQualificationStatus>(value, true, out var qs) ? q.Where(x => x.Audit.QualificationStatus == qs) : throw new ArgumentException("Nitelendirme durumu geçersizdir."), "status" => Enum.TryParse<SupplierAuditStatus>(value, true, out var status) ? q.Where(x => x.Audit.Status == status) : throw new ArgumentException("Durum geçersizdir."), "plannedstartutc" => Date(q, f.Operator, f.Value, f.ValueTo), _ => throw new ArgumentException($"Filtrelenmesine izin verilmeyen kolon: {f.Field}") }; }
    private static IQueryable<Row> Date(IQueryable<Row> q, string op, string? a, string? b) { var from = DateTimeOffset.Parse(a ?? "", CultureInfo.InvariantCulture).ToUniversalTime(); var to = string.IsNullOrWhiteSpace(b) ? from.AddDays(1) : DateTimeOffset.Parse(b, CultureInfo.InvariantCulture).ToUniversalTime(); return op switch { "before" => q.Where(x => x.Audit.PlannedStartUtc < from), "after" => q.Where(x => x.Audit.PlannedStartUtc > from), _ => q.Where(x => x.Audit.PlannedStartUtc >= from && x.Audit.PlannedStartUtc < to) }; }
    private static IQueryable<Row> Sort(IQueryable<Row> q, string field, string direction) { var desc = direction.Equals("desc", StringComparison.OrdinalIgnoreCase); return field.ToLowerInvariant() switch { "recordnumber" => desc ? q.OrderByDescending(x => x.Number) : q.OrderBy(x => x.Number), "suppliername" => desc ? q.OrderByDescending(x => x.Audit.SupplierName) : q.OrderBy(x => x.Audit.SupplierName), "riskscore" => desc ? q.OrderByDescending(x => x.Audit.RiskScore) : q.OrderBy(x => x.Audit.RiskScore), "plannedstartutc" => desc ? q.OrderByDescending(x => x.Audit.PlannedStartUtc) : q.OrderBy(x => x.Audit.PlannedStartUtc), "status" => desc ? q.OrderByDescending(x => x.Audit.Status) : q.OrderBy(x => x.Audit.Status), _ => desc ? q.OrderByDescending(x => x.Audit.CreatedAtUtc) : q.OrderBy(x => x.Audit.CreatedAtUtc) }; }
    private async Task<long> Next(string type, int year, IDbContextTransaction tx, CancellationToken ct) { var connection = db.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = tx.GetDbTransaction(); command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@type,@year,1) ON CONFLICT (\"RecordType\",\"CalendarYear\") DO UPDATE SET \"LastValue\"=core.record_number_sequence.\"LastValue\"+1 RETURNING \"LastValue\";"; Add(command, "type", type); Add(command, "year", year); return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture); }
    private static void Add(DbCommand command, string name, object value) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
    private sealed class Row { public required SupplierAudit Audit { get; init; } public required string Number { get; init; } }
}
