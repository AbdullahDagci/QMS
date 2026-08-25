using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Deviations;
using Qms.Application.Security;
using Qms.Contracts.Common;
using Qms.Contracts.Deviations;
using Qms.Domain.AuditTrail;
using Qms.Domain.Deviations;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Deviations;

public sealed class DeviationService(QmsDbContext dbContext, TimeProvider timeProvider, ICurrentUser currentUser)
    : IDeviationService
{
    private static readonly Guid PrototypeDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000002");

    public async Task<IReadOnlyList<DeviationListItemResponse>> ListAsync(
        CancellationToken cancellationToken)
    {
        var page = await SearchAsync(new DeviationSearchRequest(PageSize: 100), cancellationToken);
        return page.Items;
    }

    public async Task<PagedResponse<DeviationListItemResponse>> SearchAsync(
        DeviationSearchRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSearchRequest(request);
        var query =
                from deviation in dbContext.Deviations.AsNoTracking()
                join record in dbContext.QualityRecords.AsNoTracking()
                    on deviation.QualityRecordId equals record.Id
                select new DeviationSearchRow
                {
                    Deviation = deviation,
                    RecordNumber = record.RecordNumber
                };

        foreach (var filter in request.Filters ?? [])
        {
            query = ApplyFilter(query, filter);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        query = ApplySorting(query, request.SortBy, request.SortDirection);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new DeviationListItemResponse(
                row.Deviation.Id,
                row.RecordNumber,
                row.Deviation.Title,
                row.Deviation.DetectedDepartment,
                row.Deviation.RiskScore,
                row.Deviation.Classification.ToString(),
                row.Deviation.CapaRequired,
                row.Deviation.Status.ToString(),
                row.Deviation.TargetDateUtc,
                row.Deviation.CreatedAtUtc,
                row.Deviation.Version))
            .ToListAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResponse<DeviationListItemResponse>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<DeviationResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await QueryResponses(id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DeviationDetailsResponse?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await GetAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var investigations = await dbContext.DeviationInvestigations
            .AsNoTracking()
            .Where(item => item.DeviationId == id)
            .OrderByDescending(item => item.CompletedAtUtc)
            .Select(item => new DeviationInvestigationResponse(
                item.Id,
                item.Method,
                item.RootCauseCategory,
                item.RootCauseDescription,
                item.Conclusion,
                item.CompletedAtUtc))
            .ToListAsync(cancellationToken);
        var batchImpacts = await dbContext.DeviationBatchImpacts
            .AsNoTracking()
            .Where(item => item.DeviationId == id)
            .OrderBy(item => item.BatchNumber)
            .Select(item => new DeviationBatchImpactResponse(
                item.Id,
                item.BatchNumber,
                item.IsAffected,
                item.IsLocked,
                item.Disposition.ToString(),
                item.Rationale,
                item.AssessedAtUtc))
            .ToListAsync(cancellationToken);
        var auditEvents = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.AggregateType == "Deviation" && item.AggregateId == id)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ToListAsync(cancellationToken);
        var auditTrail = auditEvents
            .Select(item => new DeviationAuditEventResponse(
                item.Id,
                item.AggregateVersion,
                item.EventType,
                item.ActorDisplayNameSnapshot,
                item.OccurredAtUtc,
                item.Reason,
                item.Payload.RootElement.Clone()))
            .ToList();
        var linkedCapas = await (
                from capa in dbContext.Capas.AsNoTracking()
                join qualityRecord in dbContext.QualityRecords.AsNoTracking()
                    on capa.QualityRecordId equals qualityRecord.Id
                where capa.SourceDeviationId == id
                orderby capa.CreatedAtUtc descending
                select new DeviationLinkedCapaResponse(
                    capa.Id,
                    qualityRecord.RecordNumber,
                    capa.Title,
                    capa.Owner,
                    capa.Status.ToString(),
                    capa.TargetDateUtc))
            .ToListAsync(cancellationToken);

        return new DeviationDetailsResponse(
            record,
            investigations,
            batchImpacts,
            linkedCapas,
            auditTrail,
            GetAvailableTransitions(record.Status));
    }

    public async Task<DeviationResponse> CreateAsync(
        CreateDeviationRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var sequence = await NextRecordNumberAsync(now.Year, transaction, cancellationToken);
        var recordNumber = $"SP-{now.Year}-{sequence:000000}";
        var record = QualityRecord.Create(
            recordNumber,
            "deviation",
            currentUser.Id,
            currentUser.DepartmentId ?? PrototypeDepartmentId,
            now);
        var deviation = Deviation.CreateDraft(
            record.Id,
            request.Title,
            request.Description,
            request.ExpectedState,
            request.ImmediateAction,
            request.DeviationType,
            request.DetectedDepartment,
            request.ProcessStage,
            request.OccurredAtUtc.ToUniversalTime(),
            request.DetectedAtUtc.ToUniversalTime(),
            request.Likelihood,
            request.Severity,
            request.Detectability,
            now);

        dbContext.QualityRecords.Add(record);
        dbContext.Deviations.Add(deviation);
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Deviation", deviation.Id, WorkflowTaskRoles.Initiator, currentUser.Id, currentUser.DepartmentId, now, deviation.TargetDateUtc));
        dbContext.AuditEvents.Add(CreateAuditEvent(
            deviation,
            "DeviationCreated",
            now,
            new
            {
                recordNumber,
                deviation.Title,
                deviation.Classification,
                deviation.RiskScore,
                deviation.CapaRequired
            }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapResponse(deviation, recordNumber);
    }

    public async Task<DeviationResponse?> SubmitAsync(
        Guid id,
        SubmitDeviationRequest request,
        CancellationToken cancellationToken)
    {
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);
        if (deviation is null)
        {
            return null;
        }

        await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Initiator, cancellationToken);

        var record = await dbContext.QualityRecords.SingleAsync(
            item => item.Id == deviation.QualityRecordId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        deviation.Submit(request.ExpectedVersion, now);
        record.Submit(now);
        await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Initiator, now, cancellationToken);
        await AssignRoleAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, record.CreatedByUserId, now, deviation.TargetDateUtc, cancellationToken);
        dbContext.AuditEvents.Add(CreateAuditEvent(
            deviation,
            "DeviationSubmitted",
            now,
            new
            {
                from = "Draft",
                to = deviation.Status.ToString(),
                deviation.RiskScore,
                deviation.Classification,
                deviation.CapaRequired
            }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapResponse(deviation, record.RecordNumber);
    }

    public async Task<DeviationDetailsResponse?> AddInvestigationAsync(
        Guid id,
        AddDeviationInvestigationRequest request,
        CancellationToken cancellationToken)
    {
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (deviation is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        deviation.RegisterInvestigation(request.ExpectedVersion, now);
        var investigation = DeviationInvestigation.CreateCompleted(
            deviation.Id,
            request.Method,
            request.RootCauseCategory,
            request.RootCauseDescription,
            request.Conclusion,
            currentUser.Id,
            now);
        dbContext.DeviationInvestigations.Add(investigation);
        dbContext.AuditEvents.Add(CreateAuditEvent(
            deviation,
            "DeviationInvestigationCompleted",
            now,
            new { investigation.Method, investigation.RootCauseCategory, investigation.Conclusion }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<DeviationDetailsResponse?> AddBatchImpactAsync(
        Guid id,
        AddDeviationBatchImpactRequest request,
        CancellationToken cancellationToken)
    {
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (deviation is null)
        {
            return null;
        }

        if (!Enum.TryParse<BatchDisposition>(request.Disposition, ignoreCase: true, out var disposition))
        {
            throw new ArgumentException("Geçersiz batch/seri kararı.", nameof(request.Disposition));
        }

        var now = timeProvider.GetUtcNow();
        await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        deviation.RegisterBatchImpact(request.ExpectedVersion, now);
        var impact = DeviationBatchImpact.Create(
            deviation.Id,
            request.BatchNumber,
            request.IsAffected,
            request.IsLocked,
            disposition,
            request.Rationale,
            now);
        dbContext.DeviationBatchImpacts.Add(impact);
        dbContext.AuditEvents.Add(CreateAuditEvent(
            deviation,
            "DeviationBatchImpactAssessed",
            now,
            new { impact.BatchNumber, impact.IsAffected, impact.IsLocked, impact.Disposition }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<DeviationDetailsResponse?> TransitionAsync(
        Guid id,
        TransitionDeviationRequest request,
        CancellationToken cancellationToken)
    {
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (deviation is null)
        {
            return null;
        }

        var record = await dbContext.QualityRecords.SingleAsync(
            item => item.Id == deviation.QualityRecordId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var previousStatus = deviation.Status;
        EnsureMakerChecker(record, request.Transition);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        switch (request.Transition.Trim().ToLowerInvariant())
        {
            case "start-preliminary-review":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, cancellationToken);
                deviation.StartPreliminaryReview(request.ExpectedVersion, request.Note ?? string.Empty, now);
                break;
            case "start-investigation":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, cancellationToken);
                deviation.StartInvestigation(request.ExpectedVersion, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, now, cancellationToken);
                await AssignRoleAsync("Deviation", id, WorkflowTaskRoles.Investigator, QmsRoles.Investigator, null, now, deviation.TargetDateUtc, cancellationToken);
                break;
            case "complete-investigation":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
                var hasInvestigation = await dbContext.DeviationInvestigations
                    .AnyAsync(item => item.DeviationId == id, cancellationToken);
                deviation.CompleteInvestigation(request.ExpectedVersion, hasInvestigation, now);
                break;
            case "complete-impact-assessment":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
                var hasPendingBatch = await dbContext.DeviationBatchImpacts.AnyAsync(
                    item => item.DeviationId == id && item.Disposition == BatchDisposition.Pending,
                    cancellationToken);
                deviation.CompleteImpactAssessment(request.ExpectedVersion, hasPendingBatch, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Investigator, now, cancellationToken);
                await AssignRoleAsync("Deviation", id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, record.CreatedByUserId, now, deviation.TargetDateUtc, cancellationToken);
                break;
            case "complete-quality-assessment":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Evaluator, cancellationToken);
                var hasLinkedCapa = await dbContext.Capas.AnyAsync(
                    item => item.SourceDeviationId == id,
                    cancellationToken);
                deviation.CompleteQualityAssessment(
                    request.ExpectedVersion,
                    request.Note ?? string.Empty,
                    request.EffectivenessRequired,
                    hasLinkedCapa,
                    now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Evaluator, now, cancellationToken);
                await AssignNextDeviationTaskAsync(deviation, record.CreatedByUserId, now, cancellationToken);
                break;
            case "complete-linked-actions":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, cancellationToken);
                var linkedCapasClosed = await dbContext.Capas.AnyAsync(
                        item => item.SourceDeviationId == id,
                        cancellationToken)
                    && !await dbContext.Capas.AnyAsync(
                        item => item.SourceDeviationId == id && item.Status != Qms.Domain.Capas.CapaStatus.Closed,
                        cancellationToken);
                deviation.CompleteLinkedCapa(request.ExpectedVersion, linkedCapasClosed, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, now, cancellationToken);
                await AssignNextDeviationTaskAsync(deviation, record.CreatedByUserId, now, cancellationToken);
                break;
            case "complete-effectiveness-review":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Evaluator, cancellationToken);
                deviation.CompleteEffectivenessReview(
                    request.ExpectedVersion,
                    request.IsEffective,
                    request.Note ?? string.Empty,
                    now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Evaluator, now, cancellationToken);
                await AssignNextDeviationTaskAsync(deviation, record.CreatedByUserId, now, cancellationToken);
                break;
            case "close":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Approver, cancellationToken);
                var hasOpenDependencies = await dbContext.Capas.AnyAsync(
                    item => item.SourceDeviationId == id && item.Status != Qms.Domain.Capas.CapaStatus.Closed,
                    cancellationToken);
                deviation.Close(
                    request.ExpectedVersion,
                    request.Note ?? string.Empty,
                    hasOpenDependencies,
                    now);
                record.Close(now, hasOpenDependencies);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Approver, now, cancellationToken);
                break;
            default:
                throw new ArgumentException("Bilinmeyen sapma durum geçişi.", nameof(request.Transition));
        }

        dbContext.AuditEvents.Add(CreateAuditEvent(
            deviation,
            "DeviationStatusChanged",
            now,
            new { from = previousStatus.ToString(), to = deviation.Status.ToString(), request.Transition },
            request.Note));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDetailsAsync(id, cancellationToken);
    }

    private IQueryable<DeviationResponse> QueryResponses(Guid? id = null)
    {
        var deviations = dbContext.Deviations.AsNoTracking();
        if (id.HasValue)
        {
            deviations = deviations.Where(deviation => deviation.Id == id.Value);
        }

        return
        from deviation in deviations
        join record in dbContext.QualityRecords.AsNoTracking()
            on deviation.QualityRecordId equals record.Id
        select new DeviationResponse(
            deviation.Id,
            deviation.QualityRecordId,
            record.RecordNumber,
            deviation.Title,
            deviation.Description,
            deviation.ExpectedState,
            deviation.ImmediateAction,
            deviation.DeviationType,
            deviation.DetectedDepartment,
            deviation.ProcessStage,
            deviation.OccurredAtUtc,
            deviation.DetectedAtUtc,
            deviation.TargetDateUtc,
            deviation.Likelihood,
            deviation.Severity,
            deviation.Detectability,
            deviation.RiskScore,
            deviation.Classification.ToString(),
            deviation.CapaRequired,
            deviation.Status.ToString(),
            deviation.PreliminaryReviewNote,
            deviation.QualityAssessmentNote,
            deviation.EffectivenessRequired,
            deviation.EffectivenessAssessmentNote,
            deviation.ClosureJustification,
            deviation.ClosedAtUtc,
            deviation.CreatedAtUtc,
            deviation.UpdatedAtUtc,
            deviation.Version);
    }

    private static DeviationResponse MapResponse(Deviation deviation, string recordNumber) => new(
        deviation.Id,
        deviation.QualityRecordId,
        recordNumber,
        deviation.Title,
        deviation.Description,
        deviation.ExpectedState,
        deviation.ImmediateAction,
        deviation.DeviationType,
        deviation.DetectedDepartment,
        deviation.ProcessStage,
        deviation.OccurredAtUtc,
        deviation.DetectedAtUtc,
        deviation.TargetDateUtc,
        deviation.Likelihood,
        deviation.Severity,
        deviation.Detectability,
        deviation.RiskScore,
        deviation.Classification.ToString(),
        deviation.CapaRequired,
        deviation.Status.ToString(),
        deviation.PreliminaryReviewNote,
        deviation.QualityAssessmentNote,
        deviation.EffectivenessRequired,
        deviation.EffectivenessAssessmentNote,
        deviation.ClosureJustification,
        deviation.ClosedAtUtc,
        deviation.CreatedAtUtc,
        deviation.UpdatedAtUtc,
        deviation.Version);

    private AuditEvent CreateAuditEvent(
        Deviation deviation,
        string eventType,
        DateTimeOffset occurredAtUtc,
        object payload,
        string? reason = null) => AuditEvent.Create(
        "Deviation",
        deviation.Id,
        deviation.Version,
        eventType,
        currentUser.Id,
        currentUser.DisplayName,
        occurredAtUtc,
        Guid.CreateVersion7().ToString(),
        JsonSerializer.SerializeToDocument(payload),
        reason);

    private void EnsureMakerChecker(QualityRecord record, string transition)
    {
        var approval = transition.Trim().ToLowerInvariant() is "start-preliminary-review" or "complete-quality-assessment" or "complete-effectiveness-review" or "close";
        if (approval && record.CreatedByUserId == currentUser.Id && !currentUser.IsInRole(QmsRoles.Administrator))
            throw new QmsForbiddenException("Görev ayrılığı kuralı: Kaydı oluşturan kullanıcı aynı kaydın onay adımını tamamlayamaz.");
    }

    private async Task EnsureAssignedActorAsync(string aggregateType, Guid aggregateId, string taskRole, CancellationToken ct)
    {
        if (currentUser.IsInRole(QmsRoles.Administrator)) return;
        var assignments = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId && item.TaskRole == taskRole && item.Status == WorkflowTaskStatus.Active).ToListAsync(ct);
        if (assignments.Count == 0)
        {
            EnsureFallbackTaskRole(taskRole);
            return;
        }
        if (assignments.Any(item => item.AssignedUserId == currentUser.Id)) return;
        var now = timeProvider.GetUtcNow();
        var delegated = await dbContext.Delegations.AsNoTracking().AnyAsync(item => assignments.Select(assignment => assignment.AssignedUserId).Contains(item.DelegatorUserId) && item.DelegateUserId == currentUser.Id && item.RevokedAtUtc == null && item.StartsAtUtc <= now && item.EndsAtUtc >= now && (item.Scope == "ALL" || item.Scope == "M.01"), ct);
        if (!delegated) throw new QmsForbiddenException("Bu kayıt görevi size veya etkin bir delegasyonla size atanmamış.");
    }

    private void EnsureFallbackTaskRole(string taskRole)
    {
        var allowed = taskRole switch
        {
            WorkflowTaskRoles.ProcessAuthority or WorkflowTaskRoles.Evaluator => currentUser.IsInRole(QmsRoles.QualityAssurance),
            WorkflowTaskRoles.Initiator => currentUser.IsInRole(QmsRoles.DeviationReporter) || currentUser.IsInRole(QmsRoles.QualityAssurance),
            WorkflowTaskRoles.Investigator => currentUser.IsInRole(QmsRoles.Investigator) || currentUser.IsInRole(QmsRoles.QualityAssurance),
            WorkflowTaskRoles.ActionOwner => currentUser.IsInRole(QmsRoles.ActionOwner) || currentUser.IsInRole(QmsRoles.QualityAssurance),
            WorkflowTaskRoles.Approver or WorkflowTaskRoles.QualifiedPerson => currentUser.IsInRole(QmsRoles.Approver) || currentUser.IsInRole(QmsRoles.QualifiedPerson) || currentUser.IsInRole(QmsRoles.QualityAssurance),
            _ => false
        };
        if (!allowed) throw new QmsForbiddenException("Bu aşama için gerekli kayıt görevi veya sistem rolü sizde bulunmuyor.");
    }

    private async Task CompleteTasksAsync(string aggregateType, Guid aggregateId, string taskRole, DateTimeOffset now, CancellationToken ct)
    {
        var tasks = await dbContext.WorkflowTaskAssignments.Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId && item.TaskRole == taskRole && item.Status == WorkflowTaskStatus.Active).ToListAsync(ct);
        foreach (var task in tasks) task.Complete(now);
    }

    private async Task AssignRoleAsync(string aggregateType, Guid aggregateId, string taskRole, string globalRole, Guid? excludedUserId, DateTimeOffset now, DateTimeOffset? dueAt, CancellationToken ct)
    {
        var userId = await (from userRole in dbContext.UserRoles.AsNoTracking()
                            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
                            where role.Name == globalRole && user.IsActive && user.Id != excludedUserId
                            orderby user.DisplayName
                            select user.Id).FirstOrDefaultAsync(ct);
        if (userId == Guid.Empty) return;
        var departmentId = await dbContext.Users.Where(item => item.Id == userId).Select(item => item.DepartmentId).SingleAsync(ct);
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create(aggregateType, aggregateId, taskRole, userId, departmentId, now, dueAt));
    }

    private Task AssignNextDeviationTaskAsync(Deviation deviation, Guid creatorId, DateTimeOffset now, CancellationToken ct) => deviation.Status switch
    {
        DeviationStatus.ActionImplementation => AssignRoleAsync("Deviation", deviation.Id, WorkflowTaskRoles.ProcessAuthority, QmsRoles.QualityAssurance, creatorId, now, deviation.TargetDateUtc, ct),
        DeviationStatus.EffectivenessReview => AssignRoleAsync("Deviation", deviation.Id, WorkflowTaskRoles.Evaluator, QmsRoles.QualityAssurance, creatorId, now, deviation.TargetDateUtc, ct),
        DeviationStatus.ClosureApproval => AssignRoleAsync("Deviation", deviation.Id, WorkflowTaskRoles.Approver, QmsRoles.Approver, creatorId, now, deviation.TargetDateUtc, ct),
        _ => Task.CompletedTask
    };

    private static IReadOnlyList<DeviationTransitionResponse> GetAvailableTransitions(string status) => status switch
    {
        "Submitted" => [new("start-preliminary-review", "Ön incelemeyi başlat", true)],
        "PreliminaryReview" => [new("start-investigation", "Araştırmayı başlat", false)],
        "Investigation" => [new("complete-investigation", "Araştırmayı tamamla", false)],
        "ImpactAssessment" => [new("complete-impact-assessment", "Etki değerlendirmesini tamamla", false)],
        "QualityAssessment" => [new("complete-quality-assessment", "KG değerlendirmesini tamamla", true)],
        "ActionImplementation" => [new("complete-linked-actions", "İlişkili DÖF aksiyonlarını tamamla", false)],
        "EffectivenessReview" => [new("complete-effectiveness-review", "Etkinliği doğrula", true)],
        "ClosureApproval" => [new("close", "Sapmayı kapat", true)],
        _ => []
    };

    private static IQueryable<DeviationSearchRow> ApplyFilter(
        IQueryable<DeviationSearchRow> query,
        ColumnFilterRequest filter)
    {
        var field = filter.Field.Trim().ToLowerInvariant();
        var operation = filter.Operator.Trim().ToLowerInvariant();

        return field switch
        {
            "recordnumber" => ApplyTextFilter(query, row => row.RecordNumber, operation, filter.Value),
            "title" => ApplyTextFilter(query, row => row.Deviation.Title, operation, filter.Value),
            "detecteddepartment" => ApplyTextFilter(query, row => row.Deviation.DetectedDepartment, operation, filter.Value),
            "riskscore" => ApplyNumberFilter(query, operation, filter.Value, filter.ValueTo),
            "classification" => ApplyClassificationFilter(query, operation, filter.Value, filter.Values),
            "status" => ApplyStatusFilter(query, operation, filter.Value, filter.Values),
            "caparequired" => ApplyBooleanFilter(query, operation, filter.Value),
            "targetdateutc" => ApplyDateFilter(query, operation, filter.Value, filter.ValueTo, useTargetDate: true),
            "createdatutc" => ApplyDateFilter(query, operation, filter.Value, filter.ValueTo, useTargetDate: false),
            _ => throw new ArgumentException($"Filtrelenmesine izin verilmeyen kolon: {filter.Field}")
        };
    }

    private static IQueryable<DeviationSearchRow> ApplyTextFilter(
        IQueryable<DeviationSearchRow> query,
        System.Linq.Expressions.Expression<Func<DeviationSearchRow, string>> selector,
        string operation,
        string? value)
    {
        var normalized = RequireValue(value).ToLower();
        var parameter = selector.Parameters[0];
        var lowered = System.Linq.Expressions.Expression.Call(
            selector.Body,
            typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
        System.Linq.Expressions.Expression comparison = operation switch
        {
            "contains" => System.Linq.Expressions.Expression.Call(
                lowered,
                typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                System.Linq.Expressions.Expression.Constant(normalized)),
            "startswith" => System.Linq.Expressions.Expression.Call(
                lowered,
                typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                System.Linq.Expressions.Expression.Constant(normalized)),
            "equals" => System.Linq.Expressions.Expression.Equal(
                lowered,
                System.Linq.Expressions.Expression.Constant(normalized)),
            _ => throw new ArgumentException($"Metin alanı için geçersiz operatör: {operation}")
        };
        return query.Where(System.Linq.Expressions.Expression.Lambda<Func<DeviationSearchRow, bool>>(comparison, parameter));
    }

    private static IQueryable<DeviationSearchRow> ApplyNumberFilter(
        IQueryable<DeviationSearchRow> query,
        string operation,
        string? value,
        string? valueTo)
    {
        var from = ParseInt(value);
        return operation switch
        {
            "equals" => query.Where(row => row.Deviation.RiskScore == from),
            "greaterthanorequal" => query.Where(row => row.Deviation.RiskScore >= from),
            "lessthanorequal" => query.Where(row => row.Deviation.RiskScore <= from),
            "between" => query.Where(row => row.Deviation.RiskScore >= from && row.Deviation.RiskScore <= ParseInt(valueTo)),
            _ => throw new ArgumentException($"Sayısal alan için geçersiz operatör: {operation}")
        };
    }

    private static IQueryable<DeviationSearchRow> ApplyClassificationFilter(
        IQueryable<DeviationSearchRow> query,
        string operation,
        string? value,
        IReadOnlyList<string>? values)
    {
        var parsed = ParseEnumValues<DeviationClassification>(operation, value, values);
        return query.Where(row => parsed.Contains(row.Deviation.Classification));
    }

    private static IQueryable<DeviationSearchRow> ApplyStatusFilter(
        IQueryable<DeviationSearchRow> query,
        string operation,
        string? value,
        IReadOnlyList<string>? values)
    {
        var parsed = ParseEnumValues<DeviationStatus>(operation, value, values);
        return query.Where(row => parsed.Contains(row.Deviation.Status));
    }

    private static IQueryable<DeviationSearchRow> ApplyBooleanFilter(
        IQueryable<DeviationSearchRow> query,
        string operation,
        string? value)
    {
        if (operation != "equals" || !bool.TryParse(value, out var parsed))
        {
            throw new ArgumentException("Boolean alan yalnız equals operatörü ve true/false değeri kabul eder.");
        }

        return query.Where(row => row.Deviation.CapaRequired == parsed);
    }

    private static IQueryable<DeviationSearchRow> ApplyDateFilter(
        IQueryable<DeviationSearchRow> query,
        string operation,
        string? value,
        string? valueTo,
        bool useTargetDate)
    {
        var from = ParseDate(value);
        if (operation == "on")
        {
            var until = from.AddDays(1);
            return useTargetDate
                ? query.Where(row => row.Deviation.TargetDateUtc >= from && row.Deviation.TargetDateUtc < until)
                : query.Where(row => row.Deviation.CreatedAtUtc >= from && row.Deviation.CreatedAtUtc < until);
        }

        if (operation == "before")
        {
            return useTargetDate
                ? query.Where(row => row.Deviation.TargetDateUtc < from)
                : query.Where(row => row.Deviation.CreatedAtUtc < from);
        }

        if (operation == "after")
        {
            return useTargetDate
                ? query.Where(row => row.Deviation.TargetDateUtc > from)
                : query.Where(row => row.Deviation.CreatedAtUtc > from);
        }

        if (operation == "between")
        {
            var until = ParseDate(valueTo).AddDays(1);
            return useTargetDate
                ? query.Where(row => row.Deviation.TargetDateUtc >= from && row.Deviation.TargetDateUtc < until)
                : query.Where(row => row.Deviation.CreatedAtUtc >= from && row.Deviation.CreatedAtUtc < until);
        }

        throw new ArgumentException($"Tarih alanı için geçersiz operatör: {operation}");
    }

    private static IQueryable<DeviationSearchRow> ApplySorting(
        IQueryable<DeviationSearchRow> query,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "recordnumber" => descending ? query.OrderByDescending(row => row.RecordNumber) : query.OrderBy(row => row.RecordNumber),
            "title" => descending ? query.OrderByDescending(row => row.Deviation.Title) : query.OrderBy(row => row.Deviation.Title),
            "detecteddepartment" => descending ? query.OrderByDescending(row => row.Deviation.DetectedDepartment) : query.OrderBy(row => row.Deviation.DetectedDepartment),
            "riskscore" => descending ? query.OrderByDescending(row => row.Deviation.RiskScore) : query.OrderBy(row => row.Deviation.RiskScore),
            "classification" => descending ? query.OrderByDescending(row => row.Deviation.Classification) : query.OrderBy(row => row.Deviation.Classification),
            "status" => descending ? query.OrderByDescending(row => row.Deviation.Status) : query.OrderBy(row => row.Deviation.Status),
            "caparequired" => descending ? query.OrderByDescending(row => row.Deviation.CapaRequired) : query.OrderBy(row => row.Deviation.CapaRequired),
            "targetdateutc" => descending ? query.OrderByDescending(row => row.Deviation.TargetDateUtc) : query.OrderBy(row => row.Deviation.TargetDateUtc),
            "createdatutc" => descending ? query.OrderByDescending(row => row.Deviation.CreatedAtUtc) : query.OrderBy(row => row.Deviation.CreatedAtUtc),
            _ => throw new ArgumentException($"Sıralanmasına izin verilmeyen kolon: {sortBy}")
        };
    }

    private static void ValidateSearchRequest(DeviationSearchRequest request)
    {
        if (request.Page < 1)
        {
            throw new ArgumentException("Sayfa numarası 1 veya daha büyük olmalıdır.");
        }

        if (request.PageSize is not (10 or 25 or 50 or 100))
        {
            throw new ArgumentException("Sayfa boyutu yalnız 10, 25, 50 veya 100 olabilir.");
        }

        if (!request.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Sıralama yönü asc veya desc olmalıdır.");
        }

        if ((request.Filters?.Count ?? 0) > 20)
        {
            throw new ArgumentException("Bir sorguda en fazla 20 kolon filtresi kullanılabilir.");
        }
    }

    private static List<TEnum> ParseEnumValues<TEnum>(
        string operation,
        string? value,
        IReadOnlyList<string>? values) where TEnum : struct, Enum
    {
        var rawValues = operation switch
        {
            "equals" => [RequireValue(value)],
            "in" when values is { Count: > 0 } => values,
            _ => throw new ArgumentException($"Enum alanı için geçersiz operatör: {operation}")
        };
        var parsed = new List<TEnum>(rawValues.Count);
        foreach (var rawValue in rawValues)
        {
            if (!Enum.TryParse<TEnum>(rawValue, ignoreCase: true, out var enumValue))
            {
                throw new ArgumentException($"Geçersiz {typeof(TEnum).Name} değeri: {rawValue}");
            }

            parsed.Add(enumValue);
        }

        return parsed;
    }

    private static string RequireValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Filtre değeri zorunludur.")
            : value.Trim();

    private static int ParseInt(string? value) =>
        int.TryParse(RequireValue(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException("Geçerli bir sayısal filtre değeri girilmelidir.");

    private static DateTimeOffset ParseDate(string? value) =>
        DateTimeOffset.TryParse(
            RequireValue(value),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : throw new ArgumentException("Geçerli bir ISO 8601 tarih değeri girilmelidir.");

    private sealed class DeviationSearchRow
    {
        public required Deviation Deviation { get; init; }

        public required string RecordNumber { get; init; }
    }

    private async Task<long> NextRecordNumberAsync(
        int calendarYear,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            INSERT INTO core.record_number_sequence ("RecordType", "CalendarYear", "LastValue")
            VALUES (@recordType, @calendarYear, 1)
            ON CONFLICT ("RecordType", "CalendarYear")
            DO UPDATE SET "LastValue" = core.record_number_sequence."LastValue" + 1
            RETURNING "LastValue";
            """;
        AddParameter(command, "recordType", "deviation");
        AddParameter(command, "calendarYear", calendarYear);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
