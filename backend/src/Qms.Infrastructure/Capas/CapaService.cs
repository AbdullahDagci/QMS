using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Capas;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Contracts.Capas;
using Qms.Contracts.Common;
using Qms.Domain.AuditTrail;
using Qms.Domain.Capas;
using Qms.Domain.Notifications;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Capas;

public sealed class CapaService(QmsDbContext dbContext, TimeProvider timeProvider, ICurrentUser currentUser, IElectronicSignatureService signatures) : ICapaService
{
    private static readonly Guid PrototypeDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000002");

    public async Task<CapaLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var users = await (from user in dbContext.Users.AsNoTracking()
                           join department in dbContext.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
                           from department in departments.DefaultIfEmpty()
                           where user.IsActive
                           orderby user.DisplayName
                           select new CapaUserOptionResponse(user.Id, user.DisplayName, department == null ? null : department.Name))
            .ToListAsync(cancellationToken);
        var actionOwnerIds = await UserIdsInRolesAsync([QmsRoles.ActionOwner, QmsRoles.QualityAssurance], cancellationToken);
        var evaluatorIds = await UserIdsInRolesAsync([QmsRoles.QualityAssurance], cancellationToken);
        return new(
            users.Where(x => actionOwnerIds.Contains(x.Id)).ToList(),
            users.Where(x => evaluatorIds.Contains(x.Id)).ToList(),
            ["Deviation", "Audit", "Complaint", "Risk", "Manual"],
            ["Düzeltici", "Önleyici"]);
    }

    public async Task<PagedResponse<CapaListItemResponse>> SearchAsync(CapaSearchRequest request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is not (10 or 25 or 50 or 100)) throw new ArgumentException("Sayfa ve sayfa boyutu geçersizdir.");
        var query = from capa in dbContext.Capas.AsNoTracking()
                    join record in dbContext.QualityRecords.AsNoTracking() on capa.QualityRecordId equals record.Id
                    join source in dbContext.Deviations.AsNoTracking() on capa.SourceDeviationId equals source.Id into sources
                    from source in sources.DefaultIfEmpty()
                    join sourceRecord in dbContext.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords
                    from sourceRecord in sourceRecords.DefaultIfEmpty()
                    select new CapaRow { Capa = capa, RecordNumber = record.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber };
        foreach (var filter in request.Filters ?? []) query = ApplyFilter(query, filter);
        var total = await query.LongCountAsync(cancellationToken);
        query = ApplySort(query, request.SortBy, request.SortDirection);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new CapaListItemResponse(x.Capa.Id, x.RecordNumber, x.Capa.SourceDeviationId, x.SourceRecordNumber, x.Capa.SourceType, x.Capa.Title, x.Capa.Owner, x.Capa.TargetDateUtc, x.Capa.Status.ToString(), x.Capa.Actions.Count, x.Capa.Actions.Count(a => a.Status == CapaActionStatus.Verified), x.Capa.CreatedAtUtc, x.Capa.Version))
            .ToListAsync(cancellationToken);
        return new(items, request.Page, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<CapaDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var data = await (from capa in dbContext.Capas.AsNoTracking().Include(x => x.Actions)
                          join record in dbContext.QualityRecords.AsNoTracking() on capa.QualityRecordId equals record.Id
                          join source in dbContext.Deviations.AsNoTracking() on capa.SourceDeviationId equals source.Id into sources
                          from source in sources.DefaultIfEmpty()
                          join sourceRecord in dbContext.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords
                          from sourceRecord in sourceRecords.DefaultIfEmpty()
                          where capa.Id == id
                          select new { Capa = capa, RecordNumber = record.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber }).SingleOrDefaultAsync(cancellationToken);
        if (data is null) return null;
        var auditEvents = await dbContext.AuditEvents.AsNoTracking()
            .Where(x => x.AggregateType == "Capa" && x.AggregateId == id)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
        var audit = auditEvents.Select(x => new CapaAuditEventResponse(
            x.Id,
            x.AggregateVersion,
            x.EventType,
            x.ActorDisplayNameSnapshot,
            x.OccurredAtUtc,
            x.Reason,
            x.Payload.RootElement.Clone())).ToList();
        var c = data.Capa;
        var response = new CapaResponse(c.Id, c.QualityRecordId, data.RecordNumber, c.SourceDeviationId, data.SourceRecordNumber, c.SourceType, c.Title, c.Description, c.RootCause, c.ImmediateActions, c.OwnerUserId, c.Owner, c.TargetDateUtc, c.EffectivenessRequired, c.EffectivenessMethod, c.EffectivenessSample, c.ObservationPeriodDays, c.SuccessCriteria, c.EffectivenessEvaluatorUserId, c.EffectivenessEvaluator, c.EffectivenessDueDateUtc, c.IsEffective, c.EffectivenessResult, c.ClosureNote, c.Status.ToString(), c.CreatedAtUtc, c.UpdatedAtUtc, c.ClosedAtUtc, c.Version);
        var actions = c.Actions.OrderBy(x => x.TargetDateUtc).Select(x => new CapaActionResponse(x.Id, x.ActionType, x.Description, x.OwnerUserId, x.Owner, x.TargetDateUtc, x.Status.ToString(), x.CompletionEvidence, x.VerificationNote, x.CompletedAtUtc, x.VerifiedAtUtc)).ToList();
        var signatures = await dbContext.ElectronicSignatures.AsNoTracking().Where(x => x.QualityRecordId == c.QualityRecordId).OrderByDescending(x => x.SignedAtUtc)
            .Select(x => new CapaSignatureResponse(x.Id, x.RecordVersion, x.SignerUserId, x.SignerDisplayNameSnapshot, x.Meaning, x.SignedAtUtc, x.ContentHash, x.Comment)).ToListAsync(cancellationToken);
        var taskRole = RequiredTaskRole(c.Status);
        var canTransition = taskRole is not null && await CanCurrentUserPerformAsync(id, taskRole, cancellationToken);
        return new(response, actions, audit, signatures, canTransition ? Transitions(c.Status) : []);
    }

    public async Task<CapaDetailsResponse> CreateAsync(CreateCapaRequest request, CancellationToken cancellationToken)
    {
        if (request.SourceDeviationId is Guid sourceId)
        {
            if (!await dbContext.Deviations.AnyAsync(x => x.Id == sourceId, cancellationToken)) throw new ArgumentException("Kaynak sapma bulunamadı.");
            var existing = await dbContext.Capas.AsNoTracking().Where(x => x.SourceDeviationId == sourceId).Select(x => x.Id).SingleOrDefaultAsync(cancellationToken);
            if (existing != Guid.Empty) return (await GetDetailsAsync(existing, cancellationToken))!;
        }
        var now = timeProvider.GetUtcNow();
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var sequence = await NextRecordNumberAsync(now.Year, tx, cancellationToken);
        var number = $"DÖF-{now.Year}-{sequence:000000}";
        var qr = QualityRecord.Create(number, "capa", currentUser.Id, currentUser.DepartmentId ?? PrototypeDepartmentId, now);
        var owner = await ActiveUserAsync(request.OwnerUserId, cancellationToken);
        (Guid Id, string DisplayName)? evaluator = request.EffectivenessEvaluatorUserId is Guid evaluatorId ? await ActiveUserAsync(evaluatorId, cancellationToken) : null;
        var capa = Capa.Create(qr.Id, request.SourceDeviationId, request.SourceType, request.Title, request.Description, request.RootCause, request.ImmediateActions, owner.Id, owner.DisplayName, request.TargetDateUtc.ToUniversalTime(), request.EffectivenessRequired, request.EffectivenessMethod ?? "", request.EffectivenessSample ?? "", request.ObservationPeriodDays, request.SuccessCriteria ?? "", evaluator?.Id, evaluator?.DisplayName ?? "", now);
        dbContext.QualityRecords.Add(qr); dbContext.Capas.Add(capa); dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Capa", capa.Id, WorkflowTaskRoles.Initiator, currentUser.Id, currentUser.DepartmentId, now, capa.TargetDateUtc, assignedUserNameSnapshot: currentUser.DisplayName, assignedDepartmentNameSnapshot: await DepartmentNameAsync(currentUser.DepartmentId, cancellationToken))); dbContext.AuditEvents.Add(Audit(capa, "CapaCreated", now, new { number, request.SourceDeviationId, ownerUserId = owner.Id }));
        await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        return (await GetDetailsAsync(capa.Id, cancellationToken))!;
    }

    public async Task<CapaDetailsResponse?> AddActionAsync(Guid id, AddCapaActionRequest request, CancellationToken ct) { await EnsureAssignedActorAsync(id, WorkflowTaskRoles.ProcessAuthority, ct); var owner = await ActiveUserAsync(request.OwnerUserId, ct); return await Mutate(id, ct, (c, now) => { c.AddAction(request.ExpectedVersion, request.ActionType, request.Description, owner.Id, owner.DisplayName, request.TargetDateUtc.ToUniversalTime(), now); dbContext.CapaActions.Add(c.Actions.Last()); return ("CapaActionAdded", (object)new { request.ActionType, ownerUserId = owner.Id, owner = owner.DisplayName }, (string?)null); }); }
    public async Task<CapaDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteCapaActionRequest request, CancellationToken ct)
    {
        await EnsureAssignedActorAsync(id, ActionTaskRole(actionId), ct);
        var capa = await dbContext.Capas.Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (capa is null) return null;
        var now = timeProvider.GetUtcNow(); var from = capa.Status; await using var tx = await dbContext.Database.BeginTransactionAsync(ct);
        capa.RequestActionCompletion(request.ExpectedVersion, actionId, request.Evidence, now);
        await CompleteTasksAsync(id, ActionTaskRole(actionId), now, ct);
        if (from != capa.Status && capa.Status == CapaStatus.ActionVerification)
            await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, null, now, capa.TargetDateUtc, ct);
        dbContext.AuditEvents.Add(Audit(capa, "CapaActionCompletionRequested", now, new { actionId, from = from.ToString(), to = capa.Status.ToString() }, request.Evidence));
        await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<CapaDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyCapaActionRequest request, CancellationToken ct)
    {
        await EnsureAssignedActorAsync(id, WorkflowTaskRoles.Evaluator, ct);
        var capa = await dbContext.Capas.Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (capa is null) return null;
        var qr = await dbContext.QualityRecords.SingleAsync(x => x.Id == capa.QualityRecordId, ct);
        var now = timeProvider.GetUtcNow(); var from = capa.Status; await using var tx = await dbContext.Database.BeginTransactionAsync(ct);
        capa.VerifyAction(request.ExpectedVersion, actionId, request.Approved, request.Note, now);
        if (!request.Approved)
        {
            await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct);
            var action = capa.Actions.Single(x => x.Id == actionId);
            if (action.OwnerUserId is Guid ownerId) await AssignUserAsync(id, ActionTaskRole(actionId), ownerId, now, action.TargetDateUtc, ct);
        }
        else if (from != capa.Status)
        {
            await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct);
            await AssignNextCapaTaskAsync(capa, qr.CreatedByUserId, now, ct);
        }
        dbContext.AuditEvents.Add(Audit(capa, "CapaActionVerified", now, new { actionId, request.Approved, from = from.ToString(), to = capa.Status.ToString() }, request.Note));
        await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    public async Task<CapaDetailsResponse?> TransitionAsync(Guid id, TransitionCapaRequest request, CancellationToken ct)
    {
        var capa = await dbContext.Capas.Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (capa is null) return null;
        var qr = await dbContext.QualityRecords.SingleAsync(x => x.Id == capa.QualityRecordId, ct);
        var now = timeProvider.GetUtcNow();
        var from = capa.Status;
        var transition = request.Transition.Trim().ToLowerInvariant();
        EnsureMakerChecker(qr, request.Transition);
        if (IsApprovalTransition(request.Transition))
            await signatures.AuthenticateAsync(request.SignaturePassword, request.SignatureMeaningAccepted, ct);
        var requiredTask = transition switch
        {
            "submit" => WorkflowTaskRoles.Initiator,
            "approve-scope" or "approve-root-cause" or "approve-plan" or "close" => WorkflowTaskRoles.Approver,
            "submit-plan" => WorkflowTaskRoles.ProcessAuthority,
            "request-action-verification" => WorkflowTaskRoles.ActionOwner,
            "approve-actions" or "start-effectiveness-review" or "complete-effectiveness" => WorkflowTaskRoles.Evaluator,
            _ => null
        };
        if (requiredTask is not null) await EnsureAssignedActorAsync(id, requiredTask, ct);
        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);
        capa.Transition(request.ExpectedVersion, request.Transition, request.Note, request.IsEffective, now);
        switch (transition)
        {
            case "submit":
                qr.Submit(now);
                await CompleteTasksAsync(id, WorkflowTaskRoles.Initiator, now, ct);
                await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, capa.TargetDateUtc, ct);
                break;
            case "approve-root-cause":
                await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct);
                await AssignRoleAsync(id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, capa.TargetDateUtc, ct);
                break;
            case "submit-plan":
                await CompleteTasksAsync(id, WorkflowTaskRoles.ProcessAuthority, now, ct);
                await AssignRoleAsync(id, WorkflowTaskRoles.Approver, QmsRoles.Approver, qr.CreatedByUserId, now, capa.TargetDateUtc, ct);
                break;
            case "approve-plan":
                await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct);
                foreach (var action in capa.Actions)
                    if (action.OwnerUserId is Guid ownerId) await AssignUserAsync(id, ActionTaskRole(action.Id), ownerId, now, action.TargetDateUtc, ct);
                break;
            case "request-action-verification":
                await CompleteTasksAsync(id, WorkflowTaskRoles.ActionOwner, now, ct);
                await AssignRoleAsync(id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, qr.CreatedByUserId, now, capa.TargetDateUtc, ct);
                break;
            case "approve-actions":
                await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct);
                await AssignNextCapaTaskAsync(capa, qr.CreatedByUserId, now, ct);
                break;
            case "complete-effectiveness":
                await CompleteTasksAsync(id, WorkflowTaskRoles.Evaluator, now, ct);
                await AssignNextCapaTaskAsync(capa, qr.CreatedByUserId, now, ct);
                break;
            case "close":
                await CompleteTasksAsync(id, WorkflowTaskRoles.Approver, now, ct);
                qr.Close(now, false);
                break;
        }
        dbContext.AuditEvents.Add(Audit(capa, "CapaStatusChanged", now, new { from = from.ToString(), to = capa.Status.ToString(), request.Transition }, request.Note));
        if (IsApprovalTransition(transition))
        {
            var meaning = SignatureMeaning(transition);
            dbContext.ElectronicSignatures.Add(signatures.CreateInternal(
                qr.Id,
                "Capa",
                capa.Id,
                capa.Version,
                transition,
                meaning,
                new { capa, actions = capa.Actions, transition, request.Note },
                now,
                request.Note));
        }
        await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    private async Task<CapaDetailsResponse?> Mutate(Guid id, CancellationToken ct, Func<Capa, DateTimeOffset, (string Type, object Payload, string? Reason)> action)
    {
        var capa = await dbContext.Capas.Include(x => x.Actions).SingleOrDefaultAsync(x => x.Id == id, ct); if (capa is null) return null;
        var now = timeProvider.GetUtcNow(); await using var tx = await dbContext.Database.BeginTransactionAsync(ct); var audit = action(capa, now);
        dbContext.AuditEvents.Add(Audit(capa, audit.Type, now, audit.Payload, audit.Reason)); await dbContext.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    private static IReadOnlyList<CapaTransitionResponse> Transitions(CapaStatus s) => s switch
    {
        CapaStatus.Draft => [new("submit", "Kapsam onayına gönder", false)], CapaStatus.ScopeApproval => [new("approve-scope", "Kapsamı onayla", false)], CapaStatus.RootCauseApproval => [new("approve-root-cause", "Kök nedeni onayla", false)], CapaStatus.ActionPlanning => [new("submit-plan", "Plan onayına gönder", false)], CapaStatus.PlanApproval => [new("approve-plan", "Planı onayla", false)], CapaStatus.Implementation => [new("request-action-verification", "KG doğrulamasına gönder", false)], CapaStatus.ActionVerification => [new("approve-actions", "Aksiyonları onayla", false)], CapaStatus.EffectivenessWaiting => [new("start-effectiveness-review", "Etkinlik incelemesini başlat", false)], CapaStatus.EffectivenessReview => [new("complete-effectiveness", "Etkinliği sonuçlandır", true)], CapaStatus.ClosureApproval => [new("close", "DÖF kaydını kapat", true)], _ => []
    };

    private static IQueryable<CapaRow> ApplyFilter(IQueryable<CapaRow> q, ColumnFilterRequest f)
    {
        var field = f.Field.Trim().ToLowerInvariant(); var op = f.Operator.Trim().ToLowerInvariant(); var value = f.Value?.Trim() ?? "";
        return field switch
        {
            "recordnumber" => op == "equals" ? q.Where(x => x.RecordNumber == value) : q.Where(x => x.RecordNumber.ToLower().Contains(value.ToLower())),
            "sourcerecordnumber" => op == "equals" ? q.Where(x => x.SourceRecordNumber == value) : q.Where(x => x.SourceRecordNumber != null && x.SourceRecordNumber.ToLower().Contains(value.ToLower())),
            "title" => op == "equals" ? q.Where(x => x.Capa.Title == value) : q.Where(x => x.Capa.Title.ToLower().Contains(value.ToLower())),
            "owner" => op == "equals" ? q.Where(x => x.Capa.Owner == value) : q.Where(x => x.Capa.Owner.ToLower().Contains(value.ToLower())),
            "status" => Enum.TryParse<CapaStatus>(value, true, out var status) ? q.Where(x => x.Capa.Status == status) : throw new ArgumentException("DÖF durumu geçersizdir."),
            "targetdateutc" => ApplyDate(q, op, f.Value, f.ValueTo, true), "createdatutc" => ApplyDate(q, op, f.Value, f.ValueTo, false),
            _ => throw new ArgumentException($"Filtrelenmesine izin verilmeyen kolon: {f.Field}")
        };
    }
    private static IQueryable<CapaRow> ApplyDate(IQueryable<CapaRow> q, string op, string? a, string? b, bool target)
    {
        var from = DateTimeOffset.Parse(a ?? "", CultureInfo.InvariantCulture).ToUniversalTime(); var to = string.IsNullOrWhiteSpace(b) ? from.AddDays(1) : DateTimeOffset.Parse(b, CultureInfo.InvariantCulture).ToUniversalTime();
        return (op, target) switch { ("before", true) => q.Where(x => x.Capa.TargetDateUtc < from), ("after", true) => q.Where(x => x.Capa.TargetDateUtc > from), (_, true) => q.Where(x => x.Capa.TargetDateUtc >= from && x.Capa.TargetDateUtc < to), ("before", false) => q.Where(x => x.Capa.CreatedAtUtc < from), ("after", false) => q.Where(x => x.Capa.CreatedAtUtc > from), _ => q.Where(x => x.Capa.CreatedAtUtc >= from && x.Capa.CreatedAtUtc < to) };
    }
    private static IQueryable<CapaRow> ApplySort(IQueryable<CapaRow> q, string field, string direction)
    {
        var desc = direction.Equals("desc", StringComparison.OrdinalIgnoreCase); return field.Trim().ToLowerInvariant() switch { "recordnumber" => desc ? q.OrderByDescending(x => x.RecordNumber) : q.OrderBy(x => x.RecordNumber), "title" => desc ? q.OrderByDescending(x => x.Capa.Title) : q.OrderBy(x => x.Capa.Title), "owner" => desc ? q.OrderByDescending(x => x.Capa.Owner) : q.OrderBy(x => x.Capa.Owner), "targetdateutc" => desc ? q.OrderByDescending(x => x.Capa.TargetDateUtc) : q.OrderBy(x => x.Capa.TargetDateUtc), "status" => desc ? q.OrderByDescending(x => x.Capa.Status) : q.OrderBy(x => x.Capa.Status), _ => desc ? q.OrderByDescending(x => x.Capa.CreatedAtUtc) : q.OrderBy(x => x.Capa.CreatedAtUtc) };
    }
    private AuditEvent Audit(Capa c, string type, DateTimeOffset now, object payload, string? reason = null) => AuditEvent.Create("Capa", c.Id, c.Version, type, currentUser.Id, currentUser.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(payload), reason);
    private void EnsureMakerChecker(QualityRecord record, string transition) { var approval = transition.Trim().ToLowerInvariant() is "approve-scope" or "approve-root-cause" or "approve-plan" or "approve-actions" or "complete-effectiveness" or "close"; if (approval && record.CreatedByUserId == currentUser.Id && !currentUser.IsInRole(QmsRoles.Administrator)) throw new QmsForbiddenException("Görev ayrılığı kuralı: Kaydı oluşturan kullanıcı aynı DÖF kaydının onayını veremez."); }
    private async Task EnsureAssignedActorAsync(Guid aggregateId, string taskRole, CancellationToken ct) { if (!await CanCurrentUserPerformAsync(aggregateId, taskRole, ct)) throw new QmsForbiddenException("Bu DÖF görevi size veya etkin bir delegasyonla size atanmamış."); }
    private async Task CompleteTasksAsync(Guid aggregateId, string taskRole, DateTimeOffset now, CancellationToken ct) { var tasks = await dbContext.WorkflowTaskAssignments.Where(item => item.AggregateType == "Capa" && item.AggregateId == aggregateId && item.TaskRole == taskRole && item.Status == WorkflowTaskStatus.Active).ToListAsync(ct); foreach (var task in tasks) task.Complete(now); }
    private async Task AssignRoleAsync(Guid aggregateId, string taskRole, string roleName, Guid? excludedUserId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct) { var userId = await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == roleName && user.IsActive && user.Id != excludedUserId orderby user.DisplayName select user.Id).FirstOrDefaultAsync(ct); if (userId == Guid.Empty) throw new InvalidOperationException($"{roleName} rolünde atanabilir etkin kullanıcı bulunamadı."); await AssignUserAsync(aggregateId, taskRole, userId, now, dueAt, ct); }
    private Task AssignNextCapaTaskAsync(Capa capa, Guid creatorId, DateTimeOffset now, CancellationToken ct) => capa.Status switch { CapaStatus.EffectivenessWaiting or CapaStatus.EffectivenessReview => AssignRoleAsync(capa.Id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, creatorId, now, capa.EffectivenessDueDateUtc ?? capa.TargetDateUtc, ct), CapaStatus.ClosureApproval => AssignRoleAsync(capa.Id, WorkflowTaskRoles.Approver, QmsRoles.Approver, creatorId, now, capa.TargetDateUtc, ct), CapaStatus.ActionPlanning => AssignRoleAsync(capa.Id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, creatorId, now, capa.TargetDateUtc, ct), _ => Task.CompletedTask };

    private async Task AssignUserAsync(Guid aggregateId, string taskRole, Guid userId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct)
    {
        var user = await (from candidate in dbContext.Users.AsNoTracking()
                          join department in dbContext.Departments.AsNoTracking() on candidate.DepartmentId equals department.Id into departments
                          from department in departments.DefaultIfEmpty()
                          where candidate.Id == userId && candidate.IsActive
                          select new { candidate.Id, candidate.DisplayName, candidate.DepartmentId, DepartmentName = department == null ? null : department.Name }).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Görev için seçilen kullanıcı etkin değil.");
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Capa", aggregateId, taskRole, user.Id, user.DepartmentId, now, dueAt, assignedUserNameSnapshot: user.DisplayName, assignedDepartmentNameSnapshot: user.DepartmentName));
        var recordNumber = await (from capa in dbContext.Capas.AsNoTracking() join qr in dbContext.QualityRecords.AsNoTracking() on capa.QualityRecordId equals qr.Id where capa.Id == aggregateId select qr.RecordNumber).SingleAsync(ct);
        dbContext.UserNotifications.Add(UserNotification.Create(user.Id, "M.02", "Yeni DÖF görevi atandı", $"{recordNumber} için {TaskRoleLabel(taskRole)} görevi size atandı.", $"/modules/m02?open={aggregateId}", now));
    }

    private async Task<bool> CanCurrentUserPerformAsync(Guid aggregateId, string taskRole, CancellationToken ct)
    {
        var owners = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "Capa" && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active).Select(x => x.AssignedUserId).ToListAsync(ct);
        if (owners.Contains(currentUser.Id)) return true;
        if (owners.Count == 0) return false;
        var now = timeProvider.GetUtcNow();
        return await dbContext.Delegations.AsNoTracking().AnyAsync(x => owners.Contains(x.DelegatorUserId) && x.DelegateUserId == currentUser.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "ALL" || x.Scope == "M.02"), ct);
    }

    private async Task<HashSet<Guid>> UserIdsInRolesAsync(string[] roles, CancellationToken ct) => (await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id where roles.Contains(role.Name!) select link.UserId).Distinct().ToListAsync(ct)).ToHashSet();
    private async Task<(Guid Id, string DisplayName)> ActiveUserAsync(Guid id, CancellationToken ct) { var user = await dbContext.Users.AsNoTracking().Where(x => x.Id == id && x.IsActive).Select(x => new { x.Id, x.DisplayName }).SingleOrDefaultAsync(ct) ?? throw new ArgumentException("Seçilen kullanıcı etkin değil veya bulunamadı."); return (user.Id, user.DisplayName); }
    private async Task<string?> DepartmentNameAsync(Guid? id, CancellationToken ct) => id is Guid departmentId ? await dbContext.Departments.AsNoTracking().Where(x => x.Id == departmentId).Select(x => x.Name).SingleOrDefaultAsync(ct) : null;
    private static string ActionTaskRole(Guid actionId) => $"ActionOwner:{actionId:N}";
    private static string TaskRoleLabel(string role) => role.StartsWith("ActionOwner:", StringComparison.Ordinal) ? "aksiyon sorumlusu" : role switch { WorkflowTaskRoles.Initiator => "başlatan", WorkflowTaskRoles.ProcessAuthority => "plan sorumlusu", WorkflowTaskRoles.Evaluator => "KG doğrulayıcısı", WorkflowTaskRoles.Approver => "onaylayan", _ => role };
    private static bool IsApprovalTransition(string transition) => transition.Trim().ToLowerInvariant() is "approve-scope" or "approve-root-cause" or "approve-plan" or "approve-actions" or "complete-effectiveness" or "close";
    private static string SignatureMeaning(string transition) => transition.Trim().ToLowerInvariant() switch { "approve-scope" => "DÖF kapsam onayı", "approve-root-cause" => "DÖF kök neden onayı", "approve-plan" => "DÖF aksiyon planı onayı", "approve-actions" => "DÖF aksiyon doğrulama onayı", "complete-effectiveness" => "DÖF etkinlik değerlendirmesi", "close" => "DÖF nihai kapanış onayı", _ => "DÖF iş akışı onayı" };
    private static string? RequiredTaskRole(CapaStatus status) => status switch { CapaStatus.Draft => WorkflowTaskRoles.Initiator, CapaStatus.ScopeApproval or CapaStatus.RootCauseApproval or CapaStatus.PlanApproval or CapaStatus.ClosureApproval => WorkflowTaskRoles.Approver, CapaStatus.ActionPlanning => WorkflowTaskRoles.ProcessAuthority, CapaStatus.ActionVerification or CapaStatus.EffectivenessWaiting or CapaStatus.EffectivenessReview => WorkflowTaskRoles.Evaluator, _ => null };
    private async Task<long> NextRecordNumberAsync(int year, IDbContextTransaction tx, CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand(); command.Transaction = tx.GetDbTransaction(); command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@recordType, @calendarYear, 1) ON CONFLICT (\"RecordType\", \"CalendarYear\") DO UPDATE SET \"LastValue\" = core.record_number_sequence.\"LastValue\" + 1 RETURNING \"LastValue\";"; Add(command, "recordType", "capa"); Add(command, "calendarYear", year); return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }
    private static void Add(DbCommand command, string name, object value) { var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p); }
    private sealed class CapaRow { public required Capa Capa { get; init; } public required string RecordNumber { get; init; } public string? SourceRecordNumber { get; init; } }
}
