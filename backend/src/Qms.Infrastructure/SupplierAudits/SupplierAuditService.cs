using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Security;
using Qms.Application.ElectronicSignatures;
using Qms.Application.SupplierAudits;
using Qms.Contracts.Common;
using Qms.Contracts.SupplierAudits;
using Qms.Domain.AuditTrail;
using Qms.Domain.Capas;
using Qms.Domain.QualityRecords;
using Qms.Domain.Notifications;
using Qms.Domain.SupplierAudits;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.SupplierAudits;

public sealed class SupplierAuditService(QmsDbContext db, TimeProvider time, ICurrentUser user, IElectronicSignatureService signatures) : ISupplierAuditService
{
    public async Task<SupplierAuditOptionsResponse> GetOptionsAsync(CancellationToken ct)
    {
        var people = await (from u in db.Users.AsNoTracking()
                            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into departments
                            from d in departments.DefaultIfEmpty()
                            where u.IsActive
                            orderby u.DisplayName
                            select new SupplierAuditIdentityOption(u.Id, u.DisplayName, d == null ? null : d.Name)).ToListAsync(ct);
        var definitions = await db.SupplierAuditLookupDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
        IReadOnlyList<SupplierAuditCodeOption> Values(string category) => definitions.Where(x => x.Category == category).Select(x => new SupplierAuditCodeOption(x.Code, x.Name)).ToList();
        var classifications = Enum.GetValues<SupplierAuditFindingClassification>().Select(x => new SupplierAuditCodeOption(x.ToString(), x switch { SupplierAuditFindingClassification.Critical => "Kritik", SupplierAuditFindingClassification.Major => "Majör", SupplierAuditFindingClassification.Minor => "Minör", _ => "Gözlem" })).ToList();
        return new(people, Values("Country"), Values("Criticality"), classifications);
    }

    public async Task<IReadOnlyList<SupplierAuditLookupDefinitionResponse>> ListLookupDefinitionsAsync(CancellationToken ct) =>
        await db.SupplierAuditLookupDefinitions.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.SortOrder).Select(x => new SupplierAuditLookupDefinitionResponse(x.Id, x.Category, x.Code, x.Name, x.SortOrder, x.IsActive)).ToListAsync(ct);

    public async Task<SupplierAuditLookupDefinitionResponse> CreateLookupDefinitionAsync(CreateSupplierAuditLookupDefinitionRequest r, CancellationToken ct)
    {
        EnsureLookupCategory(r.Category);
        if (await db.SupplierAuditLookupDefinitions.AnyAsync(x => x.Category == r.Category.Trim() && x.Code == r.Code.Trim(), ct)) throw new InvalidOperationException("Aynı M.09 lookup kodu zaten mevcut.");
        var now = time.GetUtcNow(); var item = SupplierAuditLookupDefinition.Create(r.Category, r.Code, r.Name, r.SortOrder, now);
        db.SupplierAuditLookupDefinitions.Add(item);
        db.AuditEvents.Add(AuditEvent.Create("SupplierAuditLookupDefinition", item.Id, 1, "SupplierAuditLookupCreated", user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { item.Category, item.Code, item.Name, item.SortOrder })));
        await db.SaveChangesAsync(ct); return new(item.Id, item.Category, item.Code, item.Name, item.SortOrder, item.IsActive);
    }

    public async Task<SupplierAuditLookupDefinitionResponse?> UpdateLookupDefinitionAsync(Guid id, UpdateSupplierAuditLookupDefinitionRequest r, CancellationToken ct)
    {
        var item = await db.SupplierAuditLookupDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return null;
        var now = time.GetUtcNow(); item.Update(r.Name, r.SortOrder, r.IsActive, now);
        db.AuditEvents.Add(AuditEvent.Create("SupplierAuditLookupDefinition", item.Id, 1, "SupplierAuditLookupUpdated", user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { item.Category, item.Code, item.Name, item.SortOrder, item.IsActive })));
        await db.SaveChangesAsync(ct); return new(item.Id, item.Category, item.Code, item.Name, item.SortOrder, item.IsActive);
    }

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
        var signatures = await db.ElectronicSignatures.AsNoTracking().Where(x => x.QualityRecordId == audit.QualityRecordId).OrderByDescending(x => x.SignedAtUtc).ToListAsync(ct);
        var availableTransitions = new List<SupplierAuditTransitionResponse>(); foreach (var transition in Transitions(audit.Status)) if (await CanAct(id, RoleForTransition(transition.Code), ct)) availableTransitions.Add(transition);
        var record = new SupplierAuditRecordResponse(audit.Id, audit.QualityRecordId, audit.SupplierEvaluationId, number, audit.SupplierCode, audit.SupplierName, audit.SupplierScope, audit.MaterialOrService, audit.Country, audit.Criticality, audit.PastPerformanceScore, audit.OpenFindingSnapshot, audit.RiskScore, audit.RiskBand, audit.RecommendedFrequencyMonths, audit.Scope, audit.Site, audit.LeadAuditorUserId, audit.LeadAuditor, audit.LeadAuditorDepartment, audit.PurchasingOwnerUserId, audit.PurchasingOwner, audit.VerifierUserId, audit.Verifier, audit.QualityApproverUserId, audit.QualityApprover, audit.PlannedStartUtc, audit.PlannedEndUtc, audit.ChecklistVersion, audit.ChecklistLockedAtUtc, audit.QualificationStatus.ToString(), audit.ResultRationale, audit.QualificationValidUntilUtc, audit.RequalificationRequired, audit.Status.ToString(), audit.CreatedAtUtc, audit.UpdatedAtUtc, audit.ClosedAtUtc, audit.Version);
        return new(record,
            audit.Checklist.OrderBy(x => x.Order).Select(x => new SupplierAuditChecklistResponse(x.Id, x.Order, x.Category, x.Question, x.Reference, x.Status.ToString(), x.Evidence, x.Note, x.AnsweredAtUtc)).ToList(),
            audit.Findings.OrderBy(x => x.Number).Select(x => new SupplierAuditFindingResponse(x.Id, x.Number, x.Title, x.Description, x.RequirementReference, x.Classification.ToString(), x.CapaRequired, x.LinkedCapaId, x.LinkedCapaId is Guid capaId ? capaNumbers.GetValueOrDefault(capaId) : null, x.OwnerUserId, x.Owner, x.ResponseDueAtUtc, x.SupplierResponse, x.Commitment, x.CommitmentDueAtUtc, x.Evidence, x.VerificationNote, x.Status.ToString(), x.CreatedAtUtc, x.ClosedAtUtc)).ToList(),
            audit.Invitations.OrderByDescending(x => x.CreatedAtUtc).Select(x => new SupplierAuditInvitationResponse(x.Id, x.RecipientEmail, x.ExpiresAtUtc, x.CreatedAtUtc, x.UsedAtUtc)).ToList(),
            events.Select(x => new SupplierAuditEventResponse(x.Id, x.AggregateVersion, x.EventType, x.ActorDisplayNameSnapshot, x.OccurredAtUtc, x.Reason, x.Payload.RootElement.Clone())).ToList(), availableTransitions,
            signatures.Select(x => new SupplierAuditSignatureResponse(x.Id, x.RecordVersion, x.SignerUserId, x.SignerDisplayNameSnapshot, x.Meaning, x.SignedAtUtc, x.ContentHash, x.Comment)).ToList());
    }

    public async Task<SupplierAuditDetailsResponse> CreateAsync(CreateSupplierAuditRequest r, CancellationToken ct)
    {
        EnsurePlan();
        if (!user.DepartmentId.HasValue) throw new InvalidOperationException("Tedarikçi denetimi oluşturan kullanıcının etkin bir bölüm ataması olmalıdır.");
        await RequireLookup("Country", r.Country, "Ülke", ct); await RequireLookup("Criticality", r.Criticality, "Kritiklik", ct);
        var leadAuditor = await ResolveUser(r.LeadAuditorUserId, "Baş denetçi", ct);
        var purchasingOwner = await ResolveUser(r.PurchasingOwnerUserId, "Satınalma sorumlusu", ct);
        var verifier = await ResolveUser(r.VerifierUserId, "Kanıt doğrulayıcısı", ct);
        var approver = await ResolveUser(r.QualityApproverUserId, "Kalite onaylayanı", ct);
        if (approver.Id == user.Id) throw new InvalidOperationException("Görev ayrılığı gereği kaydı oluşturan kullanıcı nihai kalite onaylayanı olamaz.");
        var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var number = $"TD-{now.Year}-{await Next("supplier-audit", now.Year, tx, ct):000000}"; var qualityRecord = QualityRecord.Create(number, "supplier-audit", user.Id, user.DepartmentId.Value, now);
        var audit = SupplierAudit.Create(qualityRecord.Id, r.SupplierEvaluationId, r.SupplierCode, r.SupplierName, r.SupplierScope, r.MaterialOrService, r.Country, r.Criticality, r.PastPerformanceScore, r.OpenFindingCount, r.Scope, r.Site, leadAuditor.Id, leadAuditor.DisplayName, leadAuditor.DepartmentName, purchasingOwner.Id, purchasingOwner.DisplayName, verifier.Id, verifier.DisplayName, approver.Id, approver.DisplayName, r.PlannedStartUtc.ToUniversalTime(), r.PlannedEndUtc.ToUniversalTime(), r.ChecklistVersion, r.Checklist.Select(x => (x.Category, x.Question, x.Reference)), now);
        db.QualityRecords.Add(qualityRecord); db.SupplierAudits.Add(audit);
        await Assign(audit.Id, WorkflowTaskRoles.SupplierAuditPlanner, user.Id, now, audit.PlannedStartUtc, ct);
        await Assign(audit.Id, WorkflowTaskRoles.SupplierAuditLeadAuditor, leadAuditor.Id, now, audit.PlannedEndUtc, ct);
        await Assign(audit.Id, WorkflowTaskRoles.SupplierResponder, purchasingOwner.Id, now, audit.PlannedEndUtc.AddDays(30), ct);
        await Assign(audit.Id, WorkflowTaskRoles.SupplierAuditVerifier, verifier.Id, now, audit.PlannedEndUtc.AddDays(45), ct);
        await Assign(audit.Id, WorkflowTaskRoles.SupplierQualityApprover, approver.Id, now, audit.PlannedEndUtc.AddDays(60), ct);
        db.AuditEvents.Add(Audit(audit, "SupplierAuditCreated", now, new { number, audit.SupplierCode, audit.SupplierName, audit.RiskScore, audit.RiskBand, audit.RecommendedFrequencyMonths }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (await GetDetailsAsync(audit.Id, ct))!;
    }

    public async Task<SupplierAuditDetailsResponse?> TransitionAsync(Guid id, TransitionSupplierAuditRequest r, CancellationToken ct)
    {
        var audit = await Load(id, ct); if (audit is null) return null;
        var transition = r.Transition.Trim().ToLowerInvariant(); var taskRole = RoleForTransition(transition);
        await EnsureActor(id, taskRole, ct); EnsureTransition(audit.Status);
        if (transition == "close") await signatures.AuthenticateAsync(r.SignaturePassword, r.SignatureMeaningAccepted, ct);
        var record = await db.QualityRecords.SingleAsync(x => x.Id == audit.QualityRecordId, ct);
        if (transition == "close" && record.CreatedByUserId == user.Id && !user.IsInRole(QmsRoles.Administrator)) throw new QmsForbiddenException("Görev ayrılığı: Kaydı oluşturan kullanıcı tedarikçi denetimini kapatamaz.");
        var now = time.GetUtcNow(); var from = audit.Status; await using var tx = await db.Database.BeginTransactionAsync(ct); audit.Transition(r.ExpectedVersion, transition, now);
        if (transition == "define-scope") record.Submit(now);
        if (transition == "close") { record.Close(now, false); await CompleteAllTasks(id, now, ct); db.ElectronicSignatures.Add(signatures.CreateInternal(record.Id, "SupplierAudit", audit.Id, audit.Version, transition, "Tedarikçi denetimi nihai kapanış onayı", new { audit, checklist = audit.Checklist, findings = audit.Findings, transition, r.Note }, now, r.Note)); }
        db.AuditEvents.Add(Audit(audit, "SupplierAuditStatusChanged", now, new { from = from.ToString(), to = audit.Status.ToString(), transition }, r.Note)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<SupplierAuditDetailsResponse?> AnswerChecklistAsync(Guid id, Guid itemId, AnswerSupplierAuditChecklistRequest r, CancellationToken ct) { await EnsureActor(id, WorkflowTaskRoles.SupplierAuditLeadAuditor, ct); return await Mutate(id, ct, (audit, now) => { EnsureExecute(); if (!Enum.TryParse<SupplierAuditChecklistStatus>(r.Status, true, out var status)) throw new ArgumentException("Soru sonucu geçersizdir."); audit.Answer(r.ExpectedVersion, itemId, status, r.Evidence, r.Note, now); return ("SupplierAuditChecklistAnswered", (object)new { itemId, status }, r.Note); }); }

    public async Task<SupplierAuditDetailsResponse?> AddFindingAsync(Guid id, AddSupplierAuditFindingRequest r, CancellationToken ct)
    {
        EnsureExecute(); await EnsureActor(id, WorkflowTaskRoles.SupplierAuditLeadAuditor, ct); if (!Enum.TryParse<SupplierAuditFindingClassification>(r.Classification, true, out var classification)) throw new ArgumentException("Bulgu sınıfı geçersizdir."); var owner = await ResolveUser(r.OwnerUserId, "Bulgu sorumlusu", ct); var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var needsCapa = r.CapaRequired || classification is SupplierAuditFindingClassification.Critical or SupplierAuditFindingClassification.Major; Guid? capaId = null;
        if (needsCapa)
        {
            var creatorDepartment = user.DepartmentId ?? throw new InvalidOperationException("DÖF açacak kullanıcının organizasyon bölümü zorunludur."); var capaNumber = $"DÖF-{now.Year}-{await Next("capa", now.Year, tx, ct):000000}"; var qualityRecord = QualityRecord.Create(capaNumber, "capa", user.Id, creatorDepartment, now);
            var capa = Capa.Create(qualityRecord.Id, null, "SupplierAudit", r.Title, r.Description, $"Tedarikçi denetim bulgusu: {r.RequirementReference}", "Tedarikçi kaynağında düzeltme ve tekrar önleme", owner.Id, owner.DisplayName, r.ResponseDueAtUtc.ToUniversalTime(), true, "Tedarikçi kanıtı ve kalite doğrulaması", audit.SupplierScope, 30, "Aynı tedarikçi uygunsuzluğunun tekrar etmemesi", audit.VerifierUserId, audit.Verifier, now);
            db.QualityRecords.Add(qualityRecord); db.Capas.Add(capa); db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Capa", capa.Id, WorkflowTaskRoles.Initiator, user.Id, user.DepartmentId, now, capa.TargetDateUtc)); capaId = capa.Id;
            db.AuditEvents.Add(AuditEvent.Create("Capa", capa.Id, capa.Version, "CapaCreatedFromSupplierAuditFinding", user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { supplierAuditId = audit.Id })));
        }
        var findingNumber = $"TDB-{now.Year}-{await Next("supplier-audit-finding", now.Year, tx, ct):000000}"; var finding = SupplierAuditFinding.Create(audit.Id, findingNumber, r.Title, r.Description, r.RequirementReference, classification, needsCapa, capaId, owner.Id, owner.DisplayName, r.ResponseDueAtUtc.ToUniversalTime(), now); var previousQualification = audit.QualificationStatus; audit.AddFinding(r.ExpectedVersion, finding, now); db.SupplierAuditFindings.Add(finding); await Assign(audit.Id, $"SupplierAuditFinding:{finding.Id}", owner.Id, now, finding.ResponseDueAtUtc, ct); db.AuditEvents.Add(Audit(audit, "SupplierAuditFindingCreated", now, new { finding.Number, finding.Classification, capaId, ownerUserId = owner.Id, previousQualification, audit.QualificationStatus })); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<SupplierAuditDetailsResponse?> RespondFindingAsync(Guid id, Guid findingId, RespondSupplierAuditFindingRequest r, CancellationToken ct) { EnsureRespond(); await EnsureActor(id, WorkflowTaskRoles.SupplierResponder, ct); return await Mutate(id, ct, (audit, now) => { audit.RespondFinding(r.ExpectedVersion, findingId, r.SupplierResponse, r.Commitment, r.CommitmentDueAtUtc.ToUniversalTime(), now); return ("SupplierAuditFindingResponseSubmitted", (object)new { findingId, r.CommitmentDueAtUtc }, r.SupplierResponse); }); }
    public async Task<SupplierAuditDetailsResponse?> SubmitEvidenceAsync(Guid id, Guid findingId, SubmitSupplierAuditEvidenceRequest r, CancellationToken ct) { EnsureRespond(); await EnsureActor(id, WorkflowTaskRoles.SupplierResponder, ct); return await Mutate(id, ct, (audit, now) => { audit.SubmitFindingEvidence(r.ExpectedVersion, findingId, r.Evidence, now); return ("SupplierAuditFindingEvidenceSubmitted", (object)new { findingId }, r.Evidence); }); }

    public async Task<SupplierAuditDetailsResponse?> CloseFindingAsync(Guid id, Guid findingId, CloseSupplierAuditFindingRequest r, CancellationToken ct)
    {
        EnsureApprove(); await EnsureActor(id, WorkflowTaskRoles.SupplierAuditVerifier, ct); await signatures.AuthenticateAsync(r.SignaturePassword, r.SignatureMeaningAccepted, ct); var audit = await Load(id, ct); if (audit is null) return null; var finding = audit.Findings.SingleOrDefault(x => x.Id == findingId) ?? throw new ArgumentException("Bulgu bulunamadı."); var capaClosed = !finding.CapaRequired || finding.LinkedCapaId is Guid capaId && await db.Capas.AnyAsync(x => x.Id == capaId && x.Status == CapaStatus.Closed, ct); var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); audit.CloseFinding(r.ExpectedVersion, findingId, r.VerificationNote, capaClosed, now); db.ElectronicSignatures.Add(signatures.CreateInternal(audit.QualityRecordId, "SupplierAudit", audit.Id, audit.Version, "finding-close", $"{finding.Number} tedarikçi bulgusu kanıt doğrulama ve kapanış onayı", new { audit, finding }, now, r.VerificationNote)); db.AuditEvents.Add(Audit(audit, "SupplierAuditFindingClosed", now, new { findingId, finding.LinkedCapaId }, r.VerificationNote)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<SupplierInvitationCreatedResponse?> CreateInvitationAsync(Guid id, CreateSupplierInvitationRequest r, CancellationToken ct)
    {
        EnsureRespond(); await EnsureActor(id, WorkflowTaskRoles.SupplierResponder, ct); var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); await using var tx = await db.Database.BeginTransactionAsync(ct); var invitation = audit.CreateInvitation(r.ExpectedVersion, r.RecipientEmail, token, r.ExpiresAtUtc.ToUniversalTime(), now); db.SupplierAuditInvitations.Add(invitation); db.AuditEvents.Add(Audit(audit, "SupplierAuditInvitationCreated", now, new { invitation.RecipientEmail, invitation.ExpiresAtUtc })); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new((await GetDetailsAsync(id, ct))!, token, $"/supplier-response?token={token}");
    }

    public async Task<SupplierInvitationReceiptResponse> SubmitInvitationResponseAsync(SupplierInvitationSubmissionRequest r, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(r.Token))); var invitation = await db.SupplierAuditInvitations.SingleOrDefaultAsync(x => x.TokenHash == hash, ct) ?? throw new ArgumentException("Tedarikçi daveti geçersizdir."); var audit = await Load(invitation.SupplierAuditId, ct) ?? throw new ArgumentException("Denetim bulunamadı."); var finding = audit.Findings.SingleOrDefault(x => x.Id == r.FindingId) ?? throw new ArgumentException("Bulgu bulunamadı."); var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); invitation.Use(now); audit.RespondFinding(audit.Version, finding.Id, r.SupplierResponse, r.Commitment, r.CommitmentDueAtUtc.ToUniversalTime(), now); audit.SubmitFindingEvidence(audit.Version, finding.Id, r.Evidence, now); db.AuditEvents.Add(AuditEvent.Create("SupplierAudit", audit.Id, audit.Version, "SupplierAuditInvitationResponseAccepted", Guid.Empty, "Tedarikçi güvenli yanıt ekranı", now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { findingId = finding.Id, invitation.RecipientEmail }), r.SupplierResponse)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); var number = await db.QualityRecords.Where(x => x.Id == audit.QualityRecordId).Select(x => x.RecordNumber).SingleAsync(ct); return new(number, finding.Number, now);
    }

    public async Task<SupplierAuditDetailsResponse?> RecordResultAsync(Guid id, RecordSupplierAuditResultRequest r, CancellationToken ct) { EnsureApprove(); await EnsureActor(id, WorkflowTaskRoles.SupplierQualityApprover, ct); await signatures.AuthenticateAsync(r.SignaturePassword, r.SignatureMeaningAccepted, ct); if (!Enum.TryParse<SupplierQualificationStatus>(r.Decision, true, out var decision)) throw new ArgumentException("Tedarikçi nitelendirme kararı geçersizdir."); var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); audit.RecordResult(r.ExpectedVersion, decision, r.Rationale, r.ValidUntilUtc?.ToUniversalTime(), r.RequalificationRequired, now); db.ElectronicSignatures.Add(signatures.CreateInternal(audit.QualityRecordId, "SupplierAudit", audit.Id, audit.Version, "record-result", "Tedarikçi nitelendirme kararı", new { audit, decision, r.ValidUntilUtc, r.RequalificationRequired }, now, r.Rationale)); db.AuditEvents.Add(Audit(audit, "SupplierAuditResultRecorded", now, new { decision, r.ValidUntilUtc, r.RequalificationRequired }, r.Rationale)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct); }

    private async Task<SupplierAuditDetailsResponse?> Mutate(Guid id, CancellationToken ct, Func<SupplierAudit, DateTimeOffset, (string Type, object Payload, string? Reason)> action) { var audit = await Load(id, ct); if (audit is null) return null; var now = time.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); var result = action(audit, now); db.AuditEvents.Add(Audit(audit, result.Type, now, result.Payload, result.Reason)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct); }
    private Task<SupplierAudit?> Load(Guid id, CancellationToken ct) => db.SupplierAudits.AsSplitQuery().Include(x => x.Checklist).Include(x => x.Findings).Include(x => x.Invitations).SingleOrDefaultAsync(x => x.Id == id, ct);
    private void EnsurePlan() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.DepartmentManager)) return; throw new QmsForbiddenException("Tedarikçi denetimi planlamak için KG veya bölüm yöneticisi rolü gerekir."); }
    private void EnsureExecute() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.Investigator) || user.IsInRole(QmsRoles.Approver)) return; throw new QmsForbiddenException("Tedarikçi denetimini uygulamak için denetçi yetkisi gerekir."); }
    private void EnsureRespond() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.DepartmentManager) || user.IsInRole(QmsRoles.ActionOwner)) return; throw new QmsForbiddenException("Tedarikçi cevabı ve kanıtı için atanmış yanıt yetkisi gerekir."); }
    private void EnsureApprove() { if (user.IsInRole(QmsRoles.Administrator) || user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.Approver) || user.IsInRole(QmsRoles.QualifiedPerson)) return; throw new QmsForbiddenException("Tedarikçi bulgu ve sonuç doğrulaması için onaylayan rolü gerekir."); }
    private static void EnsureLookupCategory(string category) { if (category.Trim() is not ("Country" or "Criticality")) throw new ArgumentException("M.09 lookup kategorisi Country veya Criticality olmalıdır."); }
    private async Task RequireLookup(string category, string code, string field, CancellationToken ct) { if (!await db.SupplierAuditLookupDefinitions.AsNoTracking().AnyAsync(x => x.Category == category && x.Code == code.Trim() && x.IsActive, ct)) throw new ArgumentException($"{field} etkin M.09 lookup kayıtlarından seçilmelidir."); }
    private async Task<(Guid Id, string DisplayName, Guid? DepartmentId, string DepartmentName)> ResolveUser(Guid id, string field, CancellationToken ct)
    {
        var value = await (from u in db.Users.AsNoTracking() join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into departments from d in departments.DefaultIfEmpty() where u.Id == id && u.IsActive select new { u.Id, u.DisplayName, u.DepartmentId, DepartmentName = d == null ? "Atanmamış" : d.Name }).SingleOrDefaultAsync(ct);
        return value is null ? throw new ArgumentException($"{field} etkin sistem kullanıcılarından seçilmelidir.") : (value.Id, value.DisplayName, value.DepartmentId, value.DepartmentName);
    }
    private async Task Assign(Guid id, string role, Guid userId, DateTimeOffset now, DateTimeOffset? due, CancellationToken ct)
    {
        var target = await ResolveUser(userId, "Görev kullanıcısı", ct);
        db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("SupplierAudit", id, role, target.Id, target.DepartmentId, now, due, assignedUserNameSnapshot: target.DisplayName, assignedDepartmentNameSnapshot: target.DepartmentName));
        db.UserNotifications.Add(UserNotification.Create(target.Id, "M.09", "Yeni tedarikçi denetimi görevi", $"{TaskRoleLabel(role)} görevi size atandı.", $"/modules/m09?open={id}", now));
    }
    private async Task EnsureActor(Guid id, string role, CancellationToken ct) { if (!await CanAct(id, role, ct)) throw new QmsForbiddenException("Bu tedarikçi denetimi görevi size veya etkin bir delegasyonla size atanmamış."); }
    private async Task<bool> CanAct(Guid id, string role, CancellationToken ct)
    {
        var owners = await db.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "SupplierAudit" && x.AggregateId == id && x.TaskRole == role && x.Status == WorkflowTaskStatus.Active).Select(x => x.AssignedUserId).ToListAsync(ct);
        if (owners.Contains(user.Id)) return true; var now = time.GetUtcNow();
        return owners.Count > 0 && await db.Delegations.AsNoTracking().AnyAsync(x => owners.Contains(x.DelegatorUserId) && x.DelegateUserId == user.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "M.09" || x.Scope == "ALL"), ct);
    }
    private async Task CompleteAllTasks(Guid id, DateTimeOffset now, CancellationToken ct) { foreach (var task in await db.WorkflowTaskAssignments.Where(x => x.AggregateType == "SupplierAudit" && x.AggregateId == id && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct)) task.Complete(now); }
    private static string RoleForTransition(string transition) => transition switch { "define-scope" => WorkflowTaskRoles.SupplierAuditPlanner, "assign-auditor" or "start-audit" or "complete-audit" or "request-supplier-response" => WorkflowTaskRoles.SupplierAuditLeadAuditor, "start-verification" => WorkflowTaskRoles.SupplierResponder, "start-capa" or "record-result" => WorkflowTaskRoles.SupplierAuditVerifier, _ => WorkflowTaskRoles.SupplierQualityApprover };
    private static string TaskRoleLabel(string role) => role.StartsWith("SupplierAuditFinding:", StringComparison.Ordinal) ? "Tedarikçi bulgu sorumlusu" : role switch { WorkflowTaskRoles.SupplierAuditPlanner => "Tedarikçi denetimi planlayıcısı", WorkflowTaskRoles.SupplierAuditLeadAuditor => "Tedarikçi baş denetçisi", WorkflowTaskRoles.SupplierResponder => "Tedarikçi yanıt sorumlusu", WorkflowTaskRoles.SupplierAuditVerifier => "Tedarikçi kanıt doğrulayıcısı", WorkflowTaskRoles.SupplierQualityApprover => "Tedarikçi kalite onaylayanı", _ => role };
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
