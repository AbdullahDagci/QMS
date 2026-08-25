using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.ChangeControls;
using Qms.Application.Security;
using Qms.Contracts.ChangeControls;
using Qms.Contracts.Common;
using Qms.Domain.AuditTrail;
using Qms.Domain.ChangeControls;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.ChangeControls;

public sealed class ChangeControlService(QmsDbContext dbContext, TimeProvider timeProvider, ICurrentUser currentUser) : IChangeControlService
{
    private static readonly Guid PrototypeDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000002");

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
        var c = data.Change;
        var responseRecord = new ChangeControlResponse(c.Id, c.QualityRecordId, data.RecordNumber, c.SourceCapaId, data.SourceRecordNumber, c.ChangeType, c.Title, c.CurrentState, c.ProposedState, c.Justification, c.Scope, c.IsTemporary, c.TemporaryUntilUtc, c.Owner, c.TargetDateUtc, c.RiskLevel, c.RiskSummary, c.ProductImpact, c.SiteImpact, c.ValidationRequired, c.RegulatoryImpact, c.RollbackPlan, c.AuthorityApprovalReference, c.CommissionedAtUtc, c.PostImplementationResult, c.ClosureNote, c.Status.ToString(), c.CreatedAtUtc, c.UpdatedAtUtc, c.ClosedAtUtc, c.Version);
        var assessments = c.Assessments.OrderBy(x => x.Department).Select(x => new ChangeAssessmentResponse(x.Id, x.Department, x.Reviewer, x.Status.ToString(), x.ImpactSummary, x.RequiredActions, x.CompletedAtUtc)).ToList();
        var actions = c.Actions.OrderBy(x => x.TargetDateUtc).Select(x => new ChangeActionResponse(x.Id, x.Category, x.Description, x.Owner, x.TargetDateUtc, x.IsBlocking, x.Status.ToString(), x.CompletionEvidence, x.VerificationNote, x.CompletedAtUtc, x.VerifiedAtUtc)).ToList();
        var audit = auditEvents.Select(x => new ChangeAuditEventResponse(x.Id, x.AggregateVersion, x.EventType, x.ActorDisplayNameSnapshot, x.OccurredAtUtc, x.Reason, x.Payload.RootElement.Clone())).ToList();
        return new(responseRecord, assessments, actions, audit, Transitions(c.Status));
    }

    public async Task<ChangeControlDetailsResponse> CreateAsync(CreateChangeControlRequest request, CancellationToken ct)
    {
        if (request.SourceCapaId is Guid sourceId && !await dbContext.Capas.AnyAsync(x => x.Id == sourceId, ct)) throw new ArgumentException("Kaynak DÖF kaydı bulunamadı.");
        var now = timeProvider.GetUtcNow(); await using var tx = await dbContext.Database.BeginTransactionAsync(ct);
        var sequence = await NextRecordNumberAsync(now.Year, tx, ct); var number = $"DK-{now.Year}-{sequence:000000}";
        var qr = QualityRecord.Create(number, "change-control", currentUser.Id, currentUser.DepartmentId ?? PrototypeDepartmentId, now);
        var change = ChangeControl.Create(qr.Id, request.SourceCapaId, request.ChangeType, request.Title, request.CurrentState, request.ProposedState, request.Justification, request.Scope, request.IsTemporary, request.TemporaryUntilUtc?.ToUniversalTime(), request.Owner, request.TargetDateUtc.ToUniversalTime(), request.RiskLevel, request.RiskSummary, request.ProductImpact, request.SiteImpact, request.ValidationRequired, request.RegulatoryImpact, request.RollbackPlan, request.ImpactedDepartments ?? [], now);
        dbContext.QualityRecords.Add(qr); dbContext.ChangeControls.Add(change); dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("ChangeControl", change.Id, WorkflowTaskRoles.Initiator, currentUser.Id, currentUser.DepartmentId, now, change.TargetDateUtc)); dbContext.AuditEvents.Add(Audit(change, "ChangeControlCreated", now, new { number, request.SourceCapaId, request.ChangeType }));
        await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (await GetDetailsAsync(change.Id, ct))!;
    }

    public async Task<ChangeControlDetailsResponse?> CompleteAssessmentAsync(Guid id, Guid assessmentId, CompleteChangeAssessmentRequest request, CancellationToken ct)
    {
        await EnsureAssignedActorAsync(id, $"Assessment:{assessmentId}", ct);
        var result = await Mutate(id, ct, (c, now) => { c.CompleteAssessment(request.ExpectedVersion, assessmentId, request.Approved, request.ImpactSummary, request.RequiredActions, now); return ("ChangeAssessmentCompleted", (object)new { assessmentId, request.Approved }, request.ImpactSummary); });
        if (result is not null) { await CompleteTasksAsync(id, $"Assessment:{assessmentId}", timeProvider.GetUtcNow(), ct); await dbContext.SaveChangesAsync(ct); }
        return result;
    }

    public Task<ChangeControlDetailsResponse?> AddActionAsync(Guid id, AddChangeActionRequest request, CancellationToken ct) => Mutate(id, ct, (c, now) => { c.AddAction(request.ExpectedVersion, request.Category, request.Description, request.Owner, request.TargetDateUtc.ToUniversalTime(), request.IsBlocking, now); dbContext.ChangeImplementationActions.Add(c.Actions.Last()); return ("ChangeActionAdded", (object)new { request.Category, request.Owner }, (string?)null); });
    public async Task<ChangeControlDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteChangeActionRequest request, CancellationToken ct) { await EnsureAssignedActorAsync(id, WorkflowTaskRoles.ActionOwner, ct); return await Mutate(id, ct, (c, now) => { c.RequestActionCompletion(request.ExpectedVersion, actionId, request.Evidence, now); return ("ChangeActionCompletionRequested", (object)new { actionId }, request.Evidence); }); }
    public async Task<ChangeControlDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyChangeActionRequest request, CancellationToken ct) { EnsureFallbackTaskRole(WorkflowTaskRoles.Evaluator); return await Mutate(id, ct, (c, now) => { c.VerifyAction(request.ExpectedVersion, actionId, request.Approved, request.Note, now); return ("ChangeActionVerified", (object)new { actionId, request.Approved }, request.Note); }); }
    public async Task<ChangeControlDetailsResponse?> SetAuthorityApprovalAsync(Guid id, SetAuthorityApprovalRequest request, CancellationToken ct) { if (!currentUser.IsInRole(QmsRoles.Administrator) && !currentUser.IsInRole(QmsRoles.QualityAssurance) && !currentUser.IsInRole(QmsRoles.RegulatoryAffairs)) throw new QmsForbiddenException("Otorite onay belgesini yalnız KG veya Ruhsatlandırma kaydedebilir."); return await Mutate(id, ct, (c, now) => { c.SetAuthorityApproval(request.ExpectedVersion, request.Reference, now); return ("AuthorityApprovalRecorded", (object)new { request.Reference }, request.Reference); }); }

    public async Task<ChangeControlDetailsResponse?> TransitionAsync(Guid id, TransitionChangeControlRequest request, CancellationToken ct)
    {
        var change = await dbContext.ChangeControls.Include(x => x.Assessments).Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (change is null) return null;
        var qr = await dbContext.QualityRecords.SingleAsync(x => x.Id == change.QualityRecordId, ct); var now = timeProvider.GetUtcNow(); var from = change.Status; var transition = request.Transition.Trim().ToLowerInvariant();
        EnsureMakerChecker(qr, transition);
        var requiredTask = transition switch { "submit" => WorkflowTaskRoles.Initiator, "approve-preliminary" => WorkflowTaskRoles.ProcessAuthority, "submit-board" => WorkflowTaskRoles.Evaluator, "approve-board" => WorkflowTaskRoles.ChangeBoard, "approve-plan" or "commission" or "close" => WorkflowTaskRoles.Approver, "request-commissioning" or "verify-implementation" or "rollback" => WorkflowTaskRoles.Evaluator, _ => null };
        if (requiredTask is not null) await EnsureAssignedActorAsync(id, requiredTask, ct);
        await using var tx = await dbContext.Database.BeginTransactionAsync(ct); change.Transition(request.ExpectedVersion, transition, request.Note, request.Successful, now);
        switch (transition)
        {
            case "submit": qr.Submit(now); await CompleteTasksAsync(id, WorkflowTaskRoles.Initiator, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-preliminary": await CompleteTasksAsync(id, WorkflowTaskRoles.ProcessAuthority, now, ct); foreach (var assessment in change.Assessments) await AssignRoleAsync(id, $"Assessment:{assessment.Id}", assessment.Department == "Ruhsatlandırma" ? QmsRoles.RegulatoryAffairs : assessment.Department == "Kalite Güvence" ? QmsRoles.QualityAssurance : QmsRoles.DepartmentManager, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "submit-board": await AssignRoleAsync(id, WorkflowTaskRoles.ChangeBoard, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-board": await CompleteTasksAsync(id, WorkflowTaskRoles.ChangeBoard, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "approve-plan": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.ActionOwner, QmsRoles.ActionOwner, null, now, change.TargetDateUtc, ct); break;
            case "request-commissioning": await CompleteTasksAsync(id, WorkflowTaskRoles.ActionOwner, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "commission": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "verify-implementation": await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); if (change.Status == ChangeControlStatus.ClosureApproval) await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, change.TargetDateUtc, ct); break;
            case "close": await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct); qr.Close(now, false); break;
            case "rollback": await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct); break;
        }
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
    private void EnsureMakerChecker(QualityRecord record, string transition) { var approval = transition is "approve-preliminary" or "submit-board" or "approve-board" or "approve-plan" or "request-commissioning" or "commission" or "verify-implementation" or "close"; if (approval && record.CreatedByUserId == currentUser.Id && !currentUser.IsInRole(QmsRoles.Administrator)) throw new QmsForbiddenException("Görev ayrılığı kuralı: Kaydı oluşturan kullanıcı aynı değişikliğin değerlendirme veya onayını veremez."); }
    private async Task EnsureAssignedActorAsync(Guid aggregateId, string taskRole, CancellationToken ct) { if (currentUser.IsInRole(QmsRoles.Administrator)) return; var assignments = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct); if (assignments.Count == 0) { EnsureFallbackTaskRole(taskRole); return; } if (assignments.Any(x => x.AssignedUserId == currentUser.Id)) return; var now = timeProvider.GetUtcNow(); var owners = assignments.Select(x => x.AssignedUserId).ToList(); var delegated = await dbContext.Delegations.AsNoTracking().AnyAsync(x => owners.Contains(x.DelegatorUserId) && x.DelegateUserId == currentUser.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "ALL" || x.Scope == "M.03"), ct); if (!delegated) throw new QmsForbiddenException("Bu değişiklik görevi size veya etkin bir delegasyonla size atanmamış."); }
    private void EnsureFallbackTaskRole(string taskRole) { if (currentUser.IsInRole(QmsRoles.Administrator)) return; var allowed = taskRole.StartsWith("Assessment:", StringComparison.Ordinal) ? currentUser.IsInRole(QmsRoles.QualityAssurance) || currentUser.IsInRole(QmsRoles.DepartmentManager) || currentUser.IsInRole(QmsRoles.RegulatoryAffairs) : taskRole switch { WorkflowTaskRoles.Initiator or WorkflowTaskRoles.ProcessAuthority or WorkflowTaskRoles.Evaluator => currentUser.IsInRole(QmsRoles.QualityAssurance), WorkflowTaskRoles.ActionOwner => currentUser.IsInRole(QmsRoles.ActionOwner) || currentUser.IsInRole(QmsRoles.QualityAssurance), WorkflowTaskRoles.Approver or WorkflowTaskRoles.ChangeBoard => currentUser.IsInRole(QmsRoles.Approver) || currentUser.IsInRole(QmsRoles.QualityAssurance), _ => false }; if (!allowed) throw new QmsForbiddenException("Bu aşama için gerekli değişiklik görevi veya sistem rolü sizde bulunmuyor."); }
    private async Task CompleteTasksAsync(Guid aggregateId, string taskRole, DateTimeOffset now, CancellationToken ct) { var tasks = await dbContext.WorkflowTaskAssignments.Where(x => x.AggregateType == "ChangeControl" && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct); foreach (var task in tasks) task.Complete(now); }
    private async Task AssignRoleAsync(Guid aggregateId, string taskRole, string roleName, Guid? excludedUserId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct) { var userId = await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == roleName && user.IsActive && user.Id != excludedUserId orderby user.DisplayName select user.Id).FirstOrDefaultAsync(ct); if (userId == Guid.Empty) return; var departmentId = await dbContext.Users.Where(x => x.Id == userId).Select(x => x.DepartmentId).SingleAsync(ct); dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("ChangeControl", aggregateId, taskRole, userId, departmentId, now, dueAt)); }
    private async Task<long> NextRecordNumberAsync(int year, IDbContextTransaction tx, CancellationToken ct) { var connection = dbContext.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = tx.GetDbTransaction(); command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@recordType, @calendarYear, 1) ON CONFLICT (\"RecordType\", \"CalendarYear\") DO UPDATE SET \"LastValue\" = core.record_number_sequence.\"LastValue\" + 1 RETURNING \"LastValue\";"; Add(command, "recordType", "change-control"); Add(command, "calendarYear", year); return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture); }
    private static void Add(DbCommand command, string name, object value) { var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p); }
    private sealed class ChangeRow { public required ChangeControl Change { get; init; } public required string RecordNumber { get; init; } public string? SourceRecordNumber { get; init; } }
}
