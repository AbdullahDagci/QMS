using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.ChangeControls;
using Qms.Application.Security;
using Qms.Contracts.ChangeControls;
using Qms.Contracts.Common;
using Qms.Domain.AuditTrail;
using Qms.Domain.ChangeControls;
using Qms.Domain.ElectronicSignatures;
using Qms.Domain.Notifications;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Identity;

namespace Qms.Infrastructure.ChangeControls;

public sealed class ChangeControlService(QmsDbContext dbContext, TimeProvider timeProvider, ICurrentUser currentUser, UserManager<ApplicationUser> userManager) : IChangeControlService
{
    private static readonly HashSet<string> LookupCategories = ["ChangeType", "RiskLevel", "RegulatoryImpact", "ActionCategory"];

    public async Task<ChangeControlLookupsResponse> GetLookupsAsync(CancellationToken ct)
    {
        var users = await (from u in dbContext.Users.AsNoTracking() join d in dbContext.Departments.AsNoTracking() on u.DepartmentId equals d.Id into ds from d in ds.DefaultIfEmpty() where u.IsActive orderby u.DisplayName select new ChangeUserOptionResponse(u.Id, u.DisplayName, d == null ? null : d.Name)).ToListAsync(ct);
        var ownerIds = await UserIdsInRolesAsync([QmsRoles.ActionOwner, QmsRoles.QualityAssurance], ct);
        var departments = await dbContext.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        var options = new List<ChangeDepartmentOptionResponse>();
        foreach (var department in departments) { var reviewer = await ResolveDepartmentReviewerAsync(department.Id, null, ct); if (reviewer is not null) options.Add(new(department.Id, department.Code, department.Name, reviewer.Value.Id, reviewer.Value.Name)); }
        var definitions = await dbContext.ChangeLookupDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
        IReadOnlyList<ChangeLookupOptionResponse> Values(string category) => definitions.Where(x => x.Category == category).Select(x => new ChangeLookupOptionResponse(x.Code, x.Name)).ToList();
        return new(users.Where(x => ownerIds.Contains(x.Id)).ToList(), options, Values("ChangeType"), Values("RiskLevel"), Values("RegulatoryImpact"), Values("ActionCategory"));
    }

    public async Task<IReadOnlyList<ChangeLookupDefinitionResponse>> ListLookupDefinitionsAsync(CancellationToken ct) => await dbContext.ChangeLookupDefinitions.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new ChangeLookupDefinitionResponse(x.Id, x.Category, x.Code, x.Name, x.SortOrder, x.IsActive)).ToListAsync(ct);
    public async Task<ChangeLookupDefinitionResponse> CreateLookupDefinitionAsync(CreateChangeLookupDefinitionRequest request, CancellationToken ct)
    {
        var category = request.Category.Trim(); if (!LookupCategories.Contains(category)) throw new ArgumentException("M.03 lookup kategorisi geçersizdir.");
        if (await dbContext.ChangeLookupDefinitions.AnyAsync(x => x.Category == category && x.Code == request.Code.Trim(), ct)) throw new InvalidOperationException("Aynı kategori ve kodla bir lookup zaten mevcut.");
        var now = timeProvider.GetUtcNow(); var item = ChangeLookupDefinition.Create(category, request.Code, request.Name, request.SortOrder, now); dbContext.ChangeLookupDefinitions.Add(item);
        dbContext.AuditEvents.Add(AuditEvent.Create("ChangeLookupDefinition", item.Id, 1, "ChangeLookupCreated", currentUser.Id, currentUser.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { item.Category, item.Code, item.Name, item.SortOrder })));
        await dbContext.SaveChangesAsync(ct); return new(item.Id, item.Category, item.Code, item.Name, item.SortOrder, item.IsActive);
    }
    public async Task<ChangeLookupDefinitionResponse?> UpdateLookupDefinitionAsync(Guid id, UpdateChangeLookupDefinitionRequest request, CancellationToken ct)
    {
        var item = await dbContext.ChangeLookupDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return null; var now = timeProvider.GetUtcNow(); item.Update(request.Name, request.SortOrder, request.IsActive, now);
        dbContext.AuditEvents.Add(AuditEvent.Create("ChangeLookupDefinition", item.Id, 1, "ChangeLookupUpdated", currentUser.Id, currentUser.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { item.Category, item.Code, item.Name, item.SortOrder, item.IsActive })));
        await dbContext.SaveChangesAsync(ct); return new(item.Id, item.Category, item.Code, item.Name, item.SortOrder, item.IsActive);
    }

    public async Task<PagedResponse<ChangeControlListItemResponse>> SearchAsync(ChangeControlSearchRequest request, CancellationToken ct)
    {
        if (request.Page < 1 || request.PageSize is not (10 or 25 or 50 or 100)) throw new ArgumentException("Sayfa ve sayfa boyutu geçersizdir.");
        var query = from change in dbContext.ChangeControls.AsNoTracking()
                    join record in dbContext.QualityRecords.AsNoTracking() on change.QualityRecordId equals record.Id
                    join source in dbContext.Capas.AsNoTracking() on change.SourceCapaId equals source.Id into sources
                    from source in sources.DefaultIfEmpty()
                    join sourceRecord in dbContext.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords
                    from sourceRecord in sourceRecords.DefaultIfEmpty()
                    select new ChangeRow { Change = change, RecordNumber = record.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber };
        foreach (var filter in request.Filters ?? []) query = ApplyFilter(query, filter);
        var total = await query.LongCountAsync(ct); query = ApplySort(query, request.SortBy, request.SortDirection);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new ChangeControlListItemResponse(x.Change.Id, x.RecordNumber, x.Change.SourceCapaId, x.SourceRecordNumber, x.Change.ChangeType, x.Change.Title, x.Change.Owner, x.Change.TargetDateUtc, x.Change.RiskLevel, x.Change.RegulatoryImpact, x.Change.Status.ToString(), x.Change.Assessments.Count, x.Change.Assessments.Count(a => a.Status != ChangeAssessmentStatus.Pending), x.Change.Actions.Count, x.Change.Actions.Count(a => a.Status == ChangeImplementationActionStatus.Verified), x.Change.CreatedAtUtc, x.Change.Version)).ToListAsync(ct);
        return new(items, request.Page, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ChangeControlDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var data = await (from change in dbContext.ChangeControls.AsNoTracking().Include(x => x.Assessments).Include(x => x.Actions)
                          join record in dbContext.QualityRecords.AsNoTracking() on change.QualityRecordId equals record.Id
                          join source in dbContext.Capas.AsNoTracking() on change.SourceCapaId equals source.Id into sources
                          from source in sources.DefaultIfEmpty()
                          join sourceRecord in dbContext.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords
                          from sourceRecord in sourceRecords.DefaultIfEmpty()
                          where change.Id == id
                          select new { Change = change, RecordNumber = record.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber }).SingleOrDefaultAsync(ct);
        if (data is null) return null;
        var auditEvents = await dbContext.AuditEvents.AsNoTracking().Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == id).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct);
        var signatures = await dbContext.ElectronicSignatures.AsNoTracking().Where(x => x.QualityRecordId == data.Change.QualityRecordId).OrderBy(x => x.SignedAtUtc).Select(x => new ChangeSignatureResponse(x.Id, x.RecordVersion, x.SignerUserId, x.SignerDisplayNameSnapshot, x.Meaning, x.SignedAtUtc, x.ContentHash, x.Comment)).ToListAsync(ct);
        var c = data.Change;
        var responseRecord = new ChangeControlResponse(c.Id, c.QualityRecordId, data.RecordNumber, c.SourceCapaId, data.SourceRecordNumber, c.ChangeType, c.Title, c.CurrentState, c.ProposedState, c.Justification, c.Scope, c.IsTemporary, c.TemporaryUntilUtc, c.OwnerUserId, c.Owner, c.TargetDateUtc, c.RiskLevel, c.RiskSummary, c.ProductImpact, c.SiteImpact, c.ValidationRequired, c.RegulatoryImpact, c.RollbackPlan, c.AuthorityApprovalReference, c.CommissionedAtUtc, c.PostImplementationResult, c.ClosureNote, c.Status.ToString(), c.CreatedAtUtc, c.UpdatedAtUtc, c.ClosedAtUtc, c.Version);
        var assessments = c.Assessments.OrderBy(x => x.Department).Select(x => new ChangeAssessmentResponse(x.Id, x.DepartmentId, x.Department, x.ReviewerUserId, x.Reviewer, x.Status.ToString(), x.ImpactSummary, x.RequiredActions, x.CompletedAtUtc)).ToList();
        var actions = c.Actions.OrderBy(x => x.TargetDateUtc).Select(x => new ChangeActionResponse(x.Id, x.Category, x.Description, x.OwnerUserId, x.Owner, x.TargetDateUtc, x.IsBlocking, x.Status.ToString(), x.CompletionEvidence, x.VerificationNote, x.CompletedAtUtc, x.VerifiedAtUtc)).ToList();
        var audit = auditEvents.Select(x => new ChangeAuditEventResponse(x.Id, x.AggregateVersion, x.EventType, x.ActorDisplayNameSnapshot, x.OccurredAtUtc, x.Reason, x.Payload.RootElement.Clone())).ToList();
        var actionableTaskRoles = await GetCurrentUserTaskRolesAsync(id, ct);
        var taskRole = RequiredTaskRole(c.Status); var allowed = taskRole is not null && actionableTaskRoles.Contains(taskRole);
        return new(responseRecord, assessments, actions, audit, signatures, allowed ? Transitions(c.Status) : [], actionableTaskRoles);
    }

    public async Task<ChangeControlDetailsResponse> CreateAsync(CreateChangeControlRequest request, CancellationToken ct)
    {
        await EnsureActiveLookupAsync("ChangeType", request.ChangeType, ct); await EnsureActiveLookupAsync("RiskLevel", request.RiskLevel, ct); await EnsureActiveLookupAsync("RegulatoryImpact", request.RegulatoryImpact, ct);
        if (request.SourceCapaId is Guid sourceId && !await dbContext.Capas.AnyAsync(x => x.Id == sourceId, ct)) throw new ArgumentException("Kaynak DÖF kaydı bulunamadı.");
        var now = timeProvider.GetUtcNow(); await using var tx = await dbContext.Database.BeginTransactionAsync(ct);
        var sequence = await NextRecordNumberAsync(now.Year, tx, ct); var number = $"DK-{now.Year}-{sequence:000000}";
        var creatorDepartmentId = currentUser.DepartmentId ?? throw new InvalidOperationException("Değişiklik kaydı oluşturacak kullanıcının etkin bir organizasyon bölümü olmalıdır.");
        var qr = QualityRecord.Create(number, "change-control", currentUser.Id, creatorDepartmentId, now);
        var owner = await ActiveUserAsync(request.OwnerUserId, ct);
        var departmentIds = (request.ImpactedDepartmentIds ?? []).ToHashSet();
        var qa = await dbContext.Departments.AsNoTracking().SingleAsync(x => x.Code == "KG" && x.IsActive, ct); departmentIds.Add(qa.Id);
        if (!request.RegulatoryImpact.Equals("None", StringComparison.OrdinalIgnoreCase)) { var regulatory = await dbContext.Departments.AsNoTracking().SingleAsync(x => x.Code == "RUH" && x.IsActive, ct); departmentIds.Add(regulatory.Id); }
        var assessmentInputs = new List<(Guid, string, Guid, string)>();
        foreach (var departmentId in departmentIds) { var department = await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == departmentId && x.IsActive, ct) ?? throw new ArgumentException("Seçilen etki bölümü etkin değil."); var reviewer = await ResolveDepartmentReviewerAsync(department.Id, qr.CreatedByUserId, ct) ?? throw new InvalidOperationException($"{department.Name} için etkin değerlendirici bulunamadı."); assessmentInputs.Add((department.Id, department.Name, reviewer.Id, reviewer.Name)); }
        var change = ChangeControl.Create(qr.Id, request.SourceCapaId, request.ChangeType, request.Title, request.CurrentState, request.ProposedState, request.Justification, request.Scope, request.IsTemporary, request.TemporaryUntilUtc?.ToUniversalTime(), owner.Id, owner.Name, request.TargetDateUtc.ToUniversalTime(), request.RiskLevel, request.RiskSummary, request.ProductImpact, request.SiteImpact, request.ValidationRequired, request.RegulatoryImpact, request.RollbackPlan, assessmentInputs, now);
        dbContext.QualityRecords.Add(qr); dbContext.ChangeControls.Add(change); await AssignUserAsync(change.Id, WorkflowTaskRoles.Initiator, currentUser.Id, now, change.TargetDateUtc, ct); dbContext.AuditEvents.Add(Audit(change, "ChangeControlCreated", now, new { number, request.SourceCapaId, request.ChangeType }));
        await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (await GetDetailsAsync(change.Id, ct))!;
    }

    public async Task<ChangeControlDetailsResponse?> CompleteAssessmentAsync(Guid id, Guid assessmentId, CompleteChangeAssessmentRequest request, CancellationToken ct)
    {
        await EnsureAssignedActorAsync(id, $"Assessment:{assessmentId}", ct);
        var result = await Mutate(id, ct, (c, now) => { c.CompleteAssessment(request.ExpectedVersion, assessmentId, request.Approved, request.ImpactSummary, request.RequiredActions, now); return ("ChangeAssessmentCompleted", (object)new { assessmentId, request.Approved }, request.ImpactSummary); });
        if (result is not null) { await CompleteTasksAsync(id, $"Assessment:{assessmentId}", timeProvider.GetUtcNow(), ct); await dbContext.SaveChangesAsync(ct); }
        return result;
    }

    public async Task<ChangeControlDetailsResponse?> AddActionAsync(Guid id, AddChangeActionRequest request, CancellationToken ct) { await EnsureAssignedActorAsync(id, WorkflowTaskRoles.Approver, ct); await EnsureActiveLookupAsync("ActionCategory", request.Category, ct); var owner = await ActiveUserAsync(request.OwnerUserId, ct); return await Mutate(id, ct, (c, now) => { c.AddAction(request.ExpectedVersion, request.Category, request.Description, owner.Id, owner.Name, request.TargetDateUtc.ToUniversalTime(), request.IsBlocking, now); dbContext.ChangeImplementationActions.Add(c.Actions.Last()); return ("ChangeActionAdded", (object)new { request.Category, owner.Id, owner.Name }, (string?)null); }); }
    public async Task<ChangeControlDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteChangeActionRequest request, CancellationToken ct) { await EnsureAssignedActorAsync(id, ActionTaskRole(actionId), ct); return await Mutate(id, ct, (c, now) => { c.RequestActionCompletion(request.ExpectedVersion, actionId, request.Evidence, now); return ("ChangeActionCompletionRequested", (object)new { actionId }, request.Evidence); }); }
    public async Task<ChangeControlDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyChangeActionRequest request, CancellationToken ct) { await EnsureAssignedActorAsync(id, WorkflowTaskRoles.Evaluator, ct); return await Mutate(id, ct, (c, now) => { c.VerifyAction(request.ExpectedVersion, actionId, request.Approved, request.Note, now); return ("ChangeActionVerified", (object)new { actionId, request.Approved }, request.Note); }); }
    public async Task<ChangeControlDetailsResponse?> SetAuthorityApprovalAsync(Guid id, SetAuthorityApprovalRequest request, CancellationToken ct) { if (!currentUser.IsInRole(QmsRoles.Administrator) && !currentUser.IsInRole(QmsRoles.QualityAssurance) && !currentUser.IsInRole(QmsRoles.RegulatoryAffairs)) throw new QmsForbiddenException("Otorite onay belgesini yalnız KG veya Ruhsatlandırma kaydedebilir."); return await Mutate(id, ct, (c, now) => { c.SetAuthorityApproval(request.ExpectedVersion, request.Reference, now); return ("AuthorityApprovalRecorded", (object)new { request.Reference }, request.Reference); }); }

    public async Task<ChangeControlDetailsResponse?> TransitionAsync(Guid id, TransitionChangeControlRequest request, CancellationToken ct)
    {
        var change = await dbContext.ChangeControls.Include(x => x.Assessments).Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (change is null) return null;
        var qr = await dbContext.QualityRecords.SingleAsync(x => x.Id == change.QualityRecordId, ct); var now = timeProvider.GetUtcNow(); var from = change.Status; var transition = request.Transition.Trim().ToLowerInvariant();
        EnsureMakerChecker(qr, transition);
        await EnsureSignatureAuthenticationAsync(request, ct);
        var requiredTask = transition switch { "submit" => WorkflowTaskRoles.Initiator, "approve-preliminary" => WorkflowTaskRoles.ProcessAuthority, "submit-board" => WorkflowTaskRoles.Evaluator, "approve-board" => WorkflowTaskRoles.ChangeBoard, "approve-plan" or "commission" or "close" => WorkflowTaskRoles.Approver, "request-commissioning" or "verify-implementation" or "rollback" => WorkflowTaskRoles.Evaluator, _ => null };
        if (requiredTask is not null) await EnsureAssignedActorAsync(id, requiredTask, ct);
        await using var tx = await dbContext.Database.BeginTransactionAsync(ct); change.Transition(request.ExpectedVersion, transition, request.Note, request.Successful, now);
        switch (transition)
        {
            case "submit": qr.Submit(now); await CompleteTasksAsync(id, WorkflowTaskRoles.Initiator, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-preliminary": await CompleteTasksAsync(id, WorkflowTaskRoles.ProcessAuthority, now, ct); foreach (var assessment in change.Assessments) await AssignUserAsync(change.Id, $"Assessment:{assessment.Id}", assessment.ReviewerUserId ?? await ResolveLegacyAssessmentReviewerAsync(assessment, qr.CreatedByUserId, ct), now, change.TargetDateUtc, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "submit-board": await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.ChangeBoard, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-board": await CompleteTasksAsync(id, WorkflowTaskRoles.ChangeBoard, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-plan": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); foreach (var action in change.Actions) await AssignUserAsync(id, ActionTaskRole(action.Id), action.OwnerUserId ?? await ResolveLegacyActionOwnerAsync(action.Owner, ct), now, action.TargetDateUtc, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "request-commissioning": foreach (var action in change.Actions) await CompleteTasksAsync(id, ActionTaskRole(action.Id), now, ct); await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "commission": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "verify-implementation": await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); if (change.Status == ChangeControlStatus.ClosureApproval) await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "close": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); qr.Close(now, false); break;
            case "rollback": await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); break;
        }
        if (IsApprovalTransition(transition)) dbContext.ElectronicSignatures.Add(ElectronicSignature.Create(qr.Id, change.Version, currentUser.Id, currentUser.DisplayName, SignatureMeaning(transition), now, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { change.Id, change.Version, transition, request.Note, request.Successful })))), request.Note));
        dbContext.AuditEvents.Add(Audit(change, "ChangeControlStatusChanged", now, new { from = from.ToString(), to = change.Status.ToString(), transition }, request.Note)); await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    private async Task<ChangeControlDetailsResponse?> Mutate(Guid id, CancellationToken ct, Func<ChangeControl, DateTimeOffset, (string Type, object Payload, string? Reason)> action)
    {
        var change = await dbContext.ChangeControls.Include(x => x.Assessments).Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (change is null) return null;
        var now = timeProvider.GetUtcNow(); await using var tx = await dbContext.Database.BeginTransactionAsync(ct); var audit = action(change, now); dbContext.AuditEvents.Add(Audit(change, audit.Type, now, audit.Payload, audit.Reason)); await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    private static IReadOnlyList<ChangeTransitionResponse> Transitions(ChangeControlStatus status) => status switch
    {
        ChangeControlStatus.Draft => [new("submit", "Ön değerlendirmeye gönder", false)],
        ChangeControlStatus.PreliminaryReview => [new("approve-preliminary", "Ön değerlendirmeyi tamamla", false)],
        ChangeControlStatus.DepartmentReview => [new("submit-board", "Değişiklik kuruluna gönder", false)],
        ChangeControlStatus.BoardReview => [new("approve-board", "Kurul kararını onayla", true)],
        ChangeControlStatus.PlanApproval => [new("approve-plan", "Uygulama planını onayla", true)],
        ChangeControlStatus.Implementation => [new("request-commissioning", "Devreye alma onayına gönder", false), new("rollback", "Kontrollü geri dönüş başlat", true)],
        ChangeControlStatus.CommissioningApproval => [new("commission", "Devreye almayı onayla", true), new("rollback", "Kontrollü geri dönüş başlat", true)],
        ChangeControlStatus.PostImplementationVerification => [new("verify-implementation", "Uygulama sonucunu doğrula", true), new("rollback", "Kontrollü geri dönüş başlat", true)],
        ChangeControlStatus.ClosureApproval => [new("close", "Değişiklik kaydını kapat", true)],
        _ => []
    };

    private static IQueryable<ChangeRow> ApplyFilter(IQueryable<ChangeRow> q, ColumnFilterRequest f)
    {
        var field = f.Field.Trim().ToLowerInvariant(); var op = f.Operator.Trim().ToLowerInvariant(); var value = f.Value?.Trim() ?? "";
        return field switch
        {
            "recordnumber" => TextFilter(q, op, value, 0), "sourcerecordnumber" => TextFilter(q, op, value, 1), "title" => TextFilter(q, op, value, 2), "owner" => TextFilter(q, op, value, 3),
            "changetype" => op == "equals" ? q.Where(x => x.Change.ChangeType == value) : q.Where(x => x.Change.ChangeType.ToLower().Contains(value.ToLower())),
            "risklevel" => q.Where(x => x.Change.RiskLevel == value), "regulatoryimpact" => q.Where(x => x.Change.RegulatoryImpact == value),
            "status" => Enum.TryParse<ChangeControlStatus>(value, true, out var status) ? q.Where(x => x.Change.Status == status) : throw new ArgumentException("Değişiklik durumu geçersizdir."),
            "targetdateutc" => ApplyDate(q, op, f.Value, f.ValueTo, true), "createdatutc" => ApplyDate(q, op, f.Value, f.ValueTo, false),
            _ => throw new ArgumentException($"Filtrelenmesine izin verilmeyen kolon: {f.Field}")
        };
    }
    private static IQueryable<ChangeRow> TextFilter(IQueryable<ChangeRow> q, string op, string value, int field) => field switch { 0 => op == "equals" ? q.Where(x => x.RecordNumber == value) : q.Where(x => x.RecordNumber.ToLower().Contains(value.ToLower())), 1 => op == "equals" ? q.Where(x => x.SourceRecordNumber == value) : q.Where(x => x.SourceRecordNumber != null && x.SourceRecordNumber.ToLower().Contains(value.ToLower())), 2 => op == "equals" ? q.Where(x => x.Change.Title == value) : q.Where(x => x.Change.Title.ToLower().Contains(value.ToLower())), _ => op == "equals" ? q.Where(x => x.Change.Owner == value) : q.Where(x => x.Change.Owner.ToLower().Contains(value.ToLower())) };
    private static IQueryable<ChangeRow> ApplyDate(IQueryable<ChangeRow> q, string op, string? a, string? b, bool target) { var from = DateTimeOffset.Parse(a ?? "", CultureInfo.InvariantCulture).ToUniversalTime(); var to = string.IsNullOrWhiteSpace(b) ? from.AddDays(1) : DateTimeOffset.Parse(b, CultureInfo.InvariantCulture).ToUniversalTime(); return (op, target) switch { ("before", true) => q.Where(x => x.Change.TargetDateUtc < from), ("after", true) => q.Where(x => x.Change.TargetDateUtc > from), (_, true) => q.Where(x => x.Change.TargetDateUtc >= from && x.Change.TargetDateUtc < to), ("before", false) => q.Where(x => x.Change.CreatedAtUtc < from), ("after", false) => q.Where(x => x.Change.CreatedAtUtc > from), _ => q.Where(x => x.Change.CreatedAtUtc >= from && x.Change.CreatedAtUtc < to) }; }
    private static IQueryable<ChangeRow> ApplySort(IQueryable<ChangeRow> q, string field, string direction) { var desc = direction.Equals("desc", StringComparison.OrdinalIgnoreCase); return field.Trim().ToLowerInvariant() switch { "recordnumber" => desc ? q.OrderByDescending(x => x.RecordNumber) : q.OrderBy(x => x.RecordNumber), "title" => desc ? q.OrderByDescending(x => x.Change.Title) : q.OrderBy(x => x.Change.Title), "owner" => desc ? q.OrderByDescending(x => x.Change.Owner) : q.OrderBy(x => x.Change.Owner), "targetdateutc" => desc ? q.OrderByDescending(x => x.Change.TargetDateUtc) : q.OrderBy(x => x.Change.TargetDateUtc), "risklevel" => desc ? q.OrderByDescending(x => x.Change.RiskLevel) : q.OrderBy(x => x.Change.RiskLevel), "status" => desc ? q.OrderByDescending(x => x.Change.Status) : q.OrderBy(x => x.Change.Status), _ => desc ? q.OrderByDescending(x => x.Change.CreatedAtUtc) : q.OrderBy(x => x.Change.CreatedAtUtc) }; }
    private AuditEvent Audit(ChangeControl c, string type, DateTimeOffset now, object payload, string? reason = null) => AuditEvent.Create("ChangeControl", c.Id, c.Version, type, currentUser.Id, currentUser.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(payload), reason);
    private void EnsureMakerChecker(QualityRecord record, string transition) { var approval = transition is "approve-preliminary" or "submit-board" or "approve-board" or "approve-plan" or "request-commissioning" or "commission" or "verify-implementation" or "close"; if (approval && record.CreatedByUserId == currentUser.Id) throw new QmsForbiddenException("Görev ayrılığı kuralı: Kaydı oluşturan kullanıcı aynı değişikliğin değerlendirme veya onayını veremez."); }
    private async Task EnsureAssignedActorAsync(Guid aggregateId, string taskRole, CancellationToken ct) { if (!await CanCurrentUserPerformAsync(aggregateId, taskRole, ct)) throw new QmsForbiddenException("Bu değişiklik görevi size veya etkin bir delegasyonla size atanmamış."); }
    private async Task CompleteTasksAsync(Guid aggregateId, string taskRole, DateTimeOffset now, CancellationToken ct) { var tasks = await dbContext.WorkflowTaskAssignments.Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct); foreach (var task in tasks) task.Complete(now); }
    private async Task AssignRoleAsync(Guid aggregateId, string taskRole, string roleName, Guid? excludedUserId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct) { var userId = await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == roleName && user.IsActive && user.Id != excludedUserId orderby user.DisplayName select user.Id).FirstOrDefaultAsync(ct); if (userId == Guid.Empty) throw new InvalidOperationException($"{roleName} rolünde atanabilir etkin kullanıcı bulunamadı."); await AssignUserAsync(aggregateId, taskRole, userId, now, dueAt, ct); }
    private async Task AssignUserAsync(Guid aggregateId, string taskRole, Guid userId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct)
    {
        var user = await (from u in dbContext.Users.AsNoTracking() join d in dbContext.Departments.AsNoTracking() on u.DepartmentId equals d.Id into ds from d in ds.DefaultIfEmpty() where u.Id == userId && u.IsActive select new { u.Id, u.DisplayName, u.DepartmentId, DepartmentName = d == null ? null : d.Name }).SingleOrDefaultAsync(ct) ?? throw new InvalidOperationException("Görev için seçilen kullanıcı etkin değil.");
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("ChangeControl", aggregateId, taskRole, user.Id, user.DepartmentId, now, dueAt, assignedUserNameSnapshot: user.DisplayName, assignedDepartmentNameSnapshot: user.DepartmentName));
        dbContext.UserNotifications.Add(UserNotification.Create(user.Id, "M.03", "Yeni değişiklik kontrol görevi", $"{TaskRoleLabel(taskRole)} görevi size atandı.", $"/modules/m03?open={aggregateId}", now));
    }
    private async Task<bool> CanCurrentUserPerformAsync(Guid aggregateId, string taskRole, CancellationToken ct) { var owners = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active).Select(x => x.AssignedUserId).ToListAsync(ct); if (owners.Contains(currentUser.Id)) return true; if (owners.Count == 0) return false; var now = timeProvider.GetUtcNow(); return await dbContext.Delegations.AsNoTracking().AnyAsync(x => owners.Contains(x.DelegatorUserId) && x.DelegateUserId == currentUser.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "ALL" || x.Scope == "M.03"), ct); }
    private async Task<IReadOnlyList<string>> GetCurrentUserTaskRolesAsync(Guid aggregateId, CancellationToken ct)
    {
        var assignments = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == aggregateId && x.Status == WorkflowTaskStatus.Active).Select(x => new { x.TaskRole, x.AssignedUserId }).ToListAsync(ct);
        var roles = assignments.Where(x => x.AssignedUserId == currentUser.Id).Select(x => x.TaskRole).ToHashSet(StringComparer.Ordinal);
        var ownerIds = assignments.Select(x => x.AssignedUserId).Distinct().ToList();
        if (ownerIds.Count > 0)
        {
            var now = timeProvider.GetUtcNow();
            var delegatedOwners = await dbContext.Delegations.AsNoTracking().Where(x => ownerIds.Contains(x.DelegatorUserId) && x.DelegateUserId == currentUser.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "ALL" || x.Scope == "M.03")).Select(x => x.DelegatorUserId).ToListAsync(ct);
            foreach (var role in assignments.Where(x => delegatedOwners.Contains(x.AssignedUserId)).Select(x => x.TaskRole)) roles.Add(role);
        }
        return roles.OrderBy(x => x).ToList();
    }
    private async Task EnsureSignatureAuthenticationAsync(TransitionChangeControlRequest request, CancellationToken ct) { if (!IsApprovalTransition(request.Transition)) return; if (!request.SignatureMeaningAccepted || string.IsNullOrWhiteSpace(request.SignaturePassword)) throw new QmsForbiddenException("Elektronik imza için parola ve anlam kabulü zorunludur."); var user = await userManager.FindByIdAsync(currentUser.Id.ToString()); if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.SignaturePassword)) throw new QmsForbiddenException("Elektronik imza kimlik doğrulaması başarısız."); }
    private async Task<HashSet<Guid>> UserIdsInRolesAsync(string[] roles, CancellationToken ct) => (await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id where roles.Contains(role.Name!) select link.UserId).Distinct().ToListAsync(ct)).ToHashSet();
    private async Task<(Guid Id, string Name)> ActiveUserAsync(Guid id, CancellationToken ct) { var user = await dbContext.Users.AsNoTracking().Where(x => x.Id == id && x.IsActive).Select(x => new { x.Id, x.DisplayName }).SingleOrDefaultAsync(ct) ?? throw new ArgumentException("Seçilen kullanıcı etkin değil veya bulunamadı."); return (user.Id, user.DisplayName); }
    private async Task EnsureActiveLookupAsync(string category, string code, CancellationToken ct) { if (!await dbContext.ChangeLookupDefinitions.AsNoTracking().AnyAsync(x => x.Category == category && x.Code == code.Trim() && x.IsActive, ct)) throw new ArgumentException($"Seçilen {category} tanımı etkin lookup kayıtlarında bulunamadı."); }
    private async Task<(Guid Id, string Name)?> ResolveDepartmentReviewerAsync(Guid departmentId, Guid? excludedUserId, CancellationToken ct) { var department = await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == departmentId && x.IsActive, ct); if (department is null) return null; if (department.ManagerUserId is Guid managerId && managerId != excludedUserId) { var manager = await dbContext.Users.AsNoTracking().Where(x => x.Id == managerId && x.IsActive).Select(x => new { x.Id, x.DisplayName }).SingleOrDefaultAsync(ct); if (manager is not null) return (manager.Id, manager.DisplayName); } var roleName = department.Code == "RUH" ? QmsRoles.RegulatoryAffairs : department.Code == "KG" ? QmsRoles.QualityAssurance : QmsRoles.DepartmentManager; var candidate = await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == roleName && user.IsActive && user.DepartmentId == departmentId && user.Id != excludedUserId orderby user.DisplayName select new { user.Id, user.DisplayName }).FirstOrDefaultAsync(ct); return candidate is null ? null : (candidate.Id, candidate.DisplayName); }
    private async Task<Guid> ResolveLegacyAssessmentReviewerAsync(ChangeAssessment assessment, Guid excludedUserId, CancellationToken ct) { var departmentId = assessment.DepartmentId ?? await dbContext.Departments.AsNoTracking().Where(x => x.IsActive && x.Name == assessment.Department).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct) ?? throw new InvalidOperationException($"{assessment.Department} bölümü organizasyonda bulunamadı."); var reviewer = await ResolveDepartmentReviewerAsync(departmentId, excludedUserId, ct) ?? throw new InvalidOperationException($"{assessment.Department} için etkin değerlendirici bulunamadı."); return reviewer.Id; }
    private async Task<Guid> ResolveLegacyActionOwnerAsync(string snapshotName, CancellationToken ct) { var exact = await dbContext.Users.AsNoTracking().Where(x => x.IsActive && x.DisplayName == snapshotName).Select(x => x.Id).FirstOrDefaultAsync(ct); if (exact != Guid.Empty) return exact; var candidate = await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == QmsRoles.ActionOwner && user.IsActive orderby user.DisplayName select user.Id).FirstOrDefaultAsync(ct); return candidate == Guid.Empty ? throw new InvalidOperationException("Eski aksiyon için atanabilir etkin aksiyon sorumlusu bulunamadı.") : candidate; }
    private static string ActionTaskRole(Guid actionId) => $"ActionOwner:{actionId:N}";
    private static string TaskRoleLabel(string role) => role.StartsWith("Assessment:") ? "Bölüm değerlendirmesi" : role.StartsWith("ActionOwner:") ? "Uygulama aksiyonu" : role;
    private static bool IsApprovalTransition(string transition) => transition.Trim().ToLowerInvariant() is "approve-preliminary" or "submit-board" or "approve-board" or "approve-plan" or "request-commissioning" or "commission" or "verify-implementation" or "close" or "rollback";
    private static string SignatureMeaning(string transition) => transition.Trim().ToLowerInvariant() switch { "approve-preliminary" => "Değişiklik ön değerlendirme onayı", "submit-board" => "Bölüm etki değerlendirmeleri onayı", "approve-board" => "Değişiklik kurulu kararı", "approve-plan" => "Uygulama planı onayı", "request-commissioning" => "Devreye alma talebi", "commission" => "Devreye alma onayı", "verify-implementation" => "Uygulama sonrası doğrulama", "close" => "Değişiklik nihai kapanış onayı", "rollback" => "Kontrollü geri dönüş kararı", _ => "Değişiklik kontrol onayı" };
    private static string? RequiredTaskRole(ChangeControlStatus status) => status switch { ChangeControlStatus.Draft => WorkflowTaskRoles.Initiator, ChangeControlStatus.PreliminaryReview => WorkflowTaskRoles.ProcessAuthority, ChangeControlStatus.DepartmentReview => WorkflowTaskRoles.Evaluator, ChangeControlStatus.BoardReview => WorkflowTaskRoles.ChangeBoard, ChangeControlStatus.PlanApproval or ChangeControlStatus.CommissioningApproval or ChangeControlStatus.ClosureApproval => WorkflowTaskRoles.Approver, ChangeControlStatus.Implementation or ChangeControlStatus.PostImplementationVerification => WorkflowTaskRoles.Evaluator, _ => null };
    private async Task<long> NextRecordNumberAsync(int year, IDbContextTransaction tx, CancellationToken ct) { var connection = dbContext.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = tx.GetDbTransaction(); command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@recordType, @calendarYear, 1) ON CONFLICT (\"RecordType\", \"CalendarYear\") DO UPDATE SET \"LastValue\" = core.record_number_sequence.\"LastValue\" + 1 RETURNING \"LastValue\";"; Add(command, "recordType", "change-control"); Add(command, "calendarYear", year); return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture); }
    private static void Add(DbCommand command, string name, object value) { var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p); }
    private sealed class ChangeRow { public required ChangeControl Change { get; init; } public required string RecordNumber { get; init; } public string? SourceRecordNumber { get; init; } }
}
