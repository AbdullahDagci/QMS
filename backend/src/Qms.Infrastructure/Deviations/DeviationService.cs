using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Deviations;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Contracts.Common;
using Qms.Contracts.Deviations;
using Qms.Domain.AuditTrail;
using Qms.Domain.Deviations;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Domain.Notifications;
using Qms.Infrastructure.Security;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Deviations;

public sealed class DeviationService(QmsDbContext dbContext, TimeProvider timeProvider, ICurrentUser currentUser, IElectronicSignatureService signatures, IDeviationFinalReportService finalReportService)
    : IDeviationService
{
    public async Task<DeviationLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new DeviationDepartmentOptionResponse(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var types = await dbContext.DeviationTypeDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new DeviationTypeResponse(x.Id, x.Code, x.Name, x.SortOrder, x.IsActive)).ToListAsync(cancellationToken);
        return new DeviationLookupsResponse(departments, types);
    }

    public async Task<IReadOnlyList<DeviationTypeResponse>> ListDeviationTypesAsync(CancellationToken cancellationToken) =>
        await dbContext.DeviationTypeDefinitions.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new DeviationTypeResponse(x.Id, x.Code, x.Name, x.SortOrder, x.IsActive)).ToListAsync(cancellationToken);

    public async Task<DeviationTypeResponse> CreateDeviationTypeAsync(CreateDeviationTypeRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (await dbContext.DeviationTypeDefinitions.AnyAsync(x => x.Code == code || x.Name == name, cancellationToken))
            throw new ArgumentException("Aynı kod veya ada sahip sapma türü zaten mevcut.");
        var now = timeProvider.GetUtcNow();
        var item = DeviationTypeDefinition.Create(code, name, request.SortOrder, now);
        dbContext.DeviationTypeDefinitions.Add(item);
        dbContext.AuditEvents.Add(AuditEvent.Create("DeviationTypeDefinition", item.Id, 1, "DeviationTypeCreated", currentUser.Id, currentUser.DisplayName, now, Qms.Infrastructure.Integrity.AuditCorrelation.Current, JsonSerializer.SerializeToDocument(new { item.Code, item.Name, item.SortOrder })));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(item.Id, item.Code, item.Name, item.SortOrder, item.IsActive);
    }

    public async Task<DeviationTypeResponse?> UpdateDeviationTypeAsync(Guid id, UpdateDeviationTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await dbContext.DeviationTypeDefinitions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;
        var name = request.Name.Trim();
        if (await dbContext.DeviationTypeDefinitions.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken))
            throw new ArgumentException("Aynı ada sahip sapma türü zaten mevcut.");
        var now = timeProvider.GetUtcNow();
        item.Update(name, request.SortOrder, request.IsActive, now);
        dbContext.AuditEvents.Add(AuditEvent.Create("DeviationTypeDefinition", item.Id, 1, "DeviationTypeUpdated", currentUser.Id, currentUser.DisplayName, now, Qms.Infrastructure.Integrity.AuditCorrelation.Current, JsonSerializer.SerializeToDocument(new { item.Code, item.Name, item.SortOrder, item.IsActive })));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(item.Id, item.Code, item.Name, item.SortOrder, item.IsActive);
    }

    public async Task<IReadOnlyList<DeviationAssignmentRuleResponse>> ListAssignmentRulesAsync(CancellationToken ct) =>
        await (from rule in dbContext.DeviationAssignmentRules.AsNoTracking()
               join user in dbContext.Users.AsNoTracking() on rule.AssignedUserId equals user.Id
               orderby rule.TaskRole, rule.Priority descending
               select new DeviationAssignmentRuleResponse(rule.Id, rule.TaskRole, rule.AssignedUserId, user.DisplayName, rule.DetectedDepartment, rule.DeviationType, rule.MinimumRiskScore, rule.Priority, rule.IsActive)).ToListAsync(ct);

    public async Task<DeviationAssignmentRuleResponse> CreateAssignmentRuleAsync(SaveDeviationAssignmentRuleRequest request, CancellationToken ct)
    {
        await ValidateAssignmentRuleAsync(request, ct);
        var rule = DeviationAssignmentRule.Create(request.TaskRole, request.AssignedUserId, request.DetectedDepartment, request.DeviationType, request.MinimumRiskScore, request.Priority);
        if (!request.IsActive) rule.Update(request.TaskRole, request.AssignedUserId, request.DetectedDepartment, request.DeviationType, request.MinimumRiskScore, request.Priority, false);
        dbContext.DeviationAssignmentRules.Add(rule);
        dbContext.AuditEvents.Add(AuditEvent.Create("DeviationAssignmentRule", rule.Id, 1, "DeviationAssignmentRuleCreated", currentUser.Id, currentUser.DisplayName, timeProvider.GetUtcNow(), Qms.Infrastructure.Integrity.AuditCorrelation.Current, JsonSerializer.SerializeToDocument(request)));
        await dbContext.SaveChangesAsync(ct);
        return (await ListAssignmentRulesAsync(ct)).Single(x => x.Id == rule.Id);
    }

    public async Task<DeviationAssignmentRuleResponse?> UpdateAssignmentRuleAsync(Guid id, SaveDeviationAssignmentRuleRequest request, CancellationToken ct)
    {
        await ValidateAssignmentRuleAsync(request, ct);
        var rule = await dbContext.DeviationAssignmentRules.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rule is null) return null;
        rule.Update(request.TaskRole, request.AssignedUserId, request.DetectedDepartment, request.DeviationType, request.MinimumRiskScore, request.Priority, request.IsActive);
        dbContext.AuditEvents.Add(AuditEvent.Create("DeviationAssignmentRule", rule.Id, 1, "DeviationAssignmentRuleUpdated", currentUser.Id, currentUser.DisplayName, timeProvider.GetUtcNow(), Qms.Infrastructure.Integrity.AuditCorrelation.Current, JsonSerializer.SerializeToDocument(request)));
        await dbContext.SaveChangesAsync(ct);
        return (await ListAssignmentRulesAsync(ct)).Single(x => x.Id == rule.Id);
    }

    private async Task ValidateAssignmentRuleAsync(SaveDeviationAssignmentRuleRequest request, CancellationToken ct)
    {
        var requiredRole = RequiredGlobalRole(request.TaskRole);
        var eligible = await (from link in dbContext.UserRoles.AsNoTracking()
                              join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id
                              join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id
                              where user.Id == request.AssignedUserId && user.IsActive && role.Name == requiredRole
                              select user.Id).AnyAsync(ct);
        if (!eligible) throw new ArgumentException($"Seçilen kullanıcı etkin olmalı ve {requiredRole} rolünü taşımalıdır.");
    }

    private static string RequiredGlobalRole(string taskRole) => taskRole switch
    {
        WorkflowTaskRoles.ProcessAuthority or WorkflowTaskRoles.Evaluator => QmsRoles.QualityAssurance,
        WorkflowTaskRoles.Investigator => QmsRoles.Investigator,
        WorkflowTaskRoles.Approver => QmsRoles.Approver,
        _ => throw new ArgumentException("M.01 görev rolü geçersizdir.", nameof(taskRole))
    };

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
                join record in dbContext.VisibleQualityRecords(currentUser)
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
                item.InvestigatorUserId,
                item.InvestigatorNameSnapshot,
                item.InvestigatorDepartmentSnapshot,
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
                item.AssessedByUserId,
                item.AssessedByNameSnapshot,
                item.AssessedByDepartmentSnapshot,
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
                join qualityRecord in dbContext.VisibleQualityRecords(currentUser)
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
        var signatures = await dbContext.ElectronicSignatures.AsNoTracking()
            .Where(x => x.QualityRecordId == record.QualityRecordId)
            .OrderByDescending(x => x.SignedAtUtc)
            .Select(x => new DeviationSignatureResponse(x.Id, x.RecordVersion, x.SignerUserId, x.SignerDisplayNameSnapshot, x.Meaning, x.SignedAtUtc, x.ContentHash, x.Comment))
            .ToListAsync(cancellationToken);

        var requiredTaskRole = RequiredTaskRole(record.Status);
        var canTransition = requiredTaskRole is not null && await CanCurrentUserPerformAsync("Deviation", id, requiredTaskRole, cancellationToken);
        var canInvestigate = record.Status == nameof(DeviationStatus.Investigation) && await CanCurrentUserPerformAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
        var canAssessBatch = record.Status == nameof(DeviationStatus.ImpactAssessment) && await CanCurrentUserPerformAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);

        return new DeviationDetailsResponse(
            record,
            investigations,
            batchImpacts,
            linkedCapas,
            auditTrail,
            signatures,
            canTransition ? GetAvailableTransitions(record.Status) : [],
            canInvestigate,
            canAssessBatch,
            await PreviewNextAssigneeAsync(id, cancellationToken));
    }

    public async Task<DeviationResponse> CreateAsync(
        CreateDeviationRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var actorDepartment = await CurrentDepartmentAsync(cancellationToken);
        var detectedDepartment = request.DetectedDepartment.Trim();
        if (!await dbContext.Departments.AsNoTracking().AnyAsync(x => x.IsActive && x.Name == detectedDepartment, cancellationToken))
            throw new ArgumentException("Seçilen tespit bölümü etkin organizasyon bölümleri arasında bulunamadı.", nameof(request.DetectedDepartment));
        var deviationType = request.DeviationType.Trim();
        if (!await dbContext.DeviationTypeDefinitions.AsNoTracking().AnyAsync(x => x.IsActive && x.Name == deviationType, cancellationToken))
            throw new ArgumentException("Seçilen sapma türü etkin tanımlar arasında bulunamadı.", nameof(request.DeviationType));

        var sequence = await NextRecordNumberAsync(now.Year, transaction, cancellationToken);
        var recordNumber = $"SP-{now.Year}-{sequence:000000}";
        var record = QualityRecord.Create(
            recordNumber,
            "deviation",
            currentUser.Id,
            actorDepartment.Id,
            now);
        var deviation = Deviation.CreateDraft(
            record.Id,
            request.Title,
            request.Description,
            request.ExpectedState,
            request.ImmediateAction,
            deviationType,
            detectedDepartment,
            request.ProcessStage,
            request.OccurredAtUtc.ToUniversalTime(),
            request.DetectedAtUtc.ToUniversalTime(),
            request.Likelihood,
            request.Severity,
            request.Detectability,
            now);

        dbContext.QualityRecords.Add(record);
        dbContext.Deviations.Add(deviation);
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Deviation", deviation.Id, WorkflowTaskRoles.Initiator, currentUser.Id, actorDepartment.Id, now, deviation.TargetDateUtc, assignedUserNameSnapshot: currentUser.DisplayName, assignedDepartmentNameSnapshot: actorDepartment.Name));
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
            item => item.Id == id && dbContext.VisibleQualityRecords(currentUser)
                .Any(record => record.Id == item.QualityRecordId),
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
        await AssignRoleAsync(deviation, WorkflowTaskRoles.ProcessAuthority, record.CreatedByUserId, now, cancellationToken);
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
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id
            && dbContext.VisibleQualityRecords(currentUser).Any(record => record.Id == item.QualityRecordId), cancellationToken);
        if (deviation is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
        var actorDepartment = await CurrentDepartmentNameAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        deviation.RegisterInvestigation(request.ExpectedVersion, now);
        var investigation = DeviationInvestigation.CreateCompleted(
            deviation.Id,
            request.Method,
            request.RootCauseCategory,
            request.RootCauseDescription,
            request.Conclusion,
            currentUser.Id,
            currentUser.DisplayName,
            actorDepartment,
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
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id
            && dbContext.VisibleQualityRecords(currentUser).Any(record => record.Id == item.QualityRecordId), cancellationToken);
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
        var actorDepartment = await CurrentDepartmentNameAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        deviation.RegisterBatchImpact(request.ExpectedVersion, now);
        var impact = DeviationBatchImpact.Create(
            deviation.Id,
            request.BatchNumber,
            request.IsAffected,
            request.IsLocked,
            disposition,
            request.Rationale,
            currentUser.Id,
            currentUser.DisplayName,
            actorDepartment,
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
        var deviation = await dbContext.Deviations.SingleOrDefaultAsync(item => item.Id == id
            && dbContext.VisibleQualityRecords(currentUser).Any(record => record.Id == item.QualityRecordId), cancellationToken);
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
        if (IsApprovalTransition(request.Transition))
            await signatures.AuthenticateAsync(request.SignaturePassword, request.SignatureMeaningAccepted, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        switch (request.Transition.Trim().ToLowerInvariant())
        {
            case "start-preliminary-review":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, cancellationToken);
                deviation.CompletePreliminaryReviewAndStartInvestigation(request.ExpectedVersion, request.Note ?? string.Empty, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, now, cancellationToken);
                await AssignRoleAsync(deviation, WorkflowTaskRoles.Investigator, null, now, cancellationToken);
                break;
            case "start-investigation":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, cancellationToken);
                deviation.StartInvestigation(request.ExpectedVersion, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.ProcessAuthority, now, cancellationToken);
                await AssignRoleAsync(deviation, WorkflowTaskRoles.Investigator, null, now, cancellationToken);
                break;
            case "complete-investigation":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
                var hasInvestigation = await dbContext.DeviationInvestigations
                    .AnyAsync(item => item.DeviationId == id, cancellationToken);
                deviation.CompleteInvestigation(request.ExpectedVersion, hasInvestigation, now);
                break;
            case "complete-impact-assessment":
                await EnsureAssignedActorAsync("Deviation", id, WorkflowTaskRoles.Investigator, cancellationToken);
                var batchDecisions = await dbContext.DeviationBatchImpacts.AsNoTracking().Where(item => item.DeviationId == id).ToListAsync(cancellationToken);
                var hasPendingBatch = batchDecisions.GroupBy(item => item.BatchNumber, StringComparer.OrdinalIgnoreCase)
                    .Any(group => group.OrderByDescending(item => item.AssessedAtUtc).First().Disposition == BatchDisposition.Pending);
                deviation.CompleteImpactAssessment(request.ExpectedVersion, hasPendingBatch, now);
                await CompleteTasksAsync("Deviation", id, WorkflowTaskRoles.Investigator, now, cancellationToken);
                await AssignRoleAsync(deviation, WorkflowTaskRoles.Evaluator, record.CreatedByUserId, now, cancellationToken);
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
        if (IsApprovalTransition(request.Transition))
        {
            var meaning = SignatureMeaning(request.Transition);
            var investigations = await dbContext.DeviationInvestigations.AsNoTracking()
                .Where(item => item.DeviationId == deviation.Id).ToListAsync(cancellationToken);
            var batchImpacts = await dbContext.DeviationBatchImpacts.AsNoTracking()
                .Where(item => item.DeviationId == deviation.Id).ToListAsync(cancellationToken);
            var linkedCapas = await dbContext.Capas.AsNoTracking()
                .Where(item => item.SourceDeviationId == deviation.Id)
                .Select(item => new { item.Id, item.Status, item.Version }).ToListAsync(cancellationToken);
            dbContext.ElectronicSignatures.Add(signatures.CreateInternal(
                record.Id,
                "Deviation",
                deviation.Id,
                deviation.Version,
                request.Transition,
                meaning,
                new { deviation, investigations, batchImpacts, linkedCapas, request.Note },
                now,
                request.Note));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var details = await GetDetailsAsync(id, cancellationToken);
        if (details is not null && string.Equals(details.Record.Status, "Closed", StringComparison.Ordinal))
            await finalReportService.EnsureGeneratedAsync(details, cancellationToken);
        return details;
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
        join record in dbContext.VisibleQualityRecords(currentUser)
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
            deviation.RiskMatrixVersion,
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
        deviation.RiskMatrixVersion,
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
        Qms.Infrastructure.Integrity.AuditCorrelation.Current,
        JsonSerializer.SerializeToDocument(payload),
        reason);

    private void EnsureMakerChecker(QualityRecord record, string transition)
    {
        var approval = transition.Trim().ToLowerInvariant() is "start-preliminary-review" or "complete-quality-assessment" or "complete-effectiveness-review" or "close";
        if (approval && record.CreatedByUserId == currentUser.Id)
            throw new QmsForbiddenException("Görev ayrılığı kuralı: Kaydı oluşturan kullanıcı aynı kaydın onay adımını tamamlayamaz.");
    }

    private async Task EnsureAssignedActorAsync(string aggregateType, Guid aggregateId, string taskRole, CancellationToken ct)
    {
        var assignments = await dbContext.WorkflowTaskAssignments.AsNoTracking().Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId && item.TaskRole == taskRole && item.Status == WorkflowTaskStatus.Active).ToListAsync(ct);
        if (assignments.Count == 0) throw new QmsForbiddenException("Bu M.01 aşaması için etkin görev ataması bulunmuyor. Sistem Yöneticisi kayıt görevini açıkça atamalıdır.");
        if (assignments.Any(item => item.AssignedUserId == currentUser.Id)) return;
        var now = timeProvider.GetUtcNow();
        var delegated = await dbContext.Delegations.AsNoTracking().AnyAsync(item => assignments.Select(assignment => assignment.AssignedUserId).Contains(item.DelegatorUserId) && item.DelegateUserId == currentUser.Id && item.RevokedAtUtc == null && item.StartsAtUtc <= now && item.EndsAtUtc >= now && (item.Scope == "ALL" || item.Scope == "M.01"), ct);
        if (!delegated) throw new QmsForbiddenException("Bu kayıt görevi size veya etkin bir delegasyonla size atanmamış.");
    }

    private async Task CompleteTasksAsync(string aggregateType, Guid aggregateId, string taskRole, DateTimeOffset now, CancellationToken ct)
    {
        var tasks = await dbContext.WorkflowTaskAssignments.Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId && item.TaskRole == taskRole && item.Status == WorkflowTaskStatus.Active).ToListAsync(ct);
        foreach (var task in tasks) task.Complete(now);
    }

    private async Task AssignRoleAsync(Deviation deviation, string taskRole, Guid? excludedUserId, DateTimeOffset now, CancellationToken ct)
    {
        var assignee = await ResolveAssigneeAsync(deviation, taskRole, excludedUserId, ct)
            ?? throw new InvalidOperationException($"{taskRole} görevi için sapma türü, bölüm ve RPN ile eşleşen etkin M.01 atama kuralı bulunamadı.");
        dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Deviation", deviation.Id, taskRole, assignee.UserId, assignee.DepartmentId, now, deviation.TargetDateUtc, assignedUserNameSnapshot: assignee.DisplayName, assignedDepartmentNameSnapshot: assignee.DepartmentName));
        var recordNumber = await dbContext.VisibleQualityRecords(currentUser).Where(x => x.Id == deviation.QualityRecordId).Select(x => x.RecordNumber).SingleAsync(ct);
        dbContext.UserNotifications.Add(UserNotification.Create(
            assignee.UserId,
            "M.01",
            "Yeni sapma görevi atandı",
            $"{recordNumber} için {TaskRoleLabel(taskRole)} görevi size atandı.",
            $"/modules/deviations?open={deviation.Id}",
            now));
    }

    private async Task<(Guid UserId, string DisplayName, Guid? DepartmentId, string? DepartmentName)?> ResolveAssigneeAsync(
        Deviation deviation, string taskRole, Guid? excludedUserId, CancellationToken ct)
    {
        var globalRole = RequiredGlobalRole(taskRole);
        var rule = await (from candidate in dbContext.DeviationAssignmentRules.AsNoTracking()
                          join user in dbContext.Users.AsNoTracking() on candidate.AssignedUserId equals user.Id
                          join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                          join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                          where candidate.IsActive && candidate.TaskRole == taskRole && user.IsActive && user.Id != excludedUserId && role.Name == globalRole
                                && (candidate.DetectedDepartment == null || candidate.DetectedDepartment == deviation.DetectedDepartment)
                                && (candidate.DeviationType == null || candidate.DeviationType == deviation.DeviationType)
                                && (candidate.MinimumRiskScore == null || deviation.RiskScore >= candidate.MinimumRiskScore)
                          orderby (candidate.DetectedDepartment != null ? 1 : 0) + (candidate.DeviationType != null ? 1 : 0) + (candidate.MinimumRiskScore != null ? 1 : 0) descending, candidate.Priority descending
                          select candidate).FirstOrDefaultAsync(ct);
        if (rule is null) return null;
        var userId = rule.AssignedUserId;
        var assignee = await (from user in dbContext.Users.AsNoTracking()
                              join department in dbContext.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
                              from department in departments.DefaultIfEmpty()
                              where user.Id == userId
                              select new { user.DepartmentId, user.DisplayName, DepartmentName = department == null ? null : department.Name }).SingleAsync(ct);
        return (userId, assignee.DisplayName, assignee.DepartmentId, assignee.DepartmentName);
    }

    // Mevcut adım tamamlanınca AssignRoleAsync'in atayacağı rol; sonraki adımı kullanıcı kararına bağlı durumlar için null.
    private static (string TaskRole, bool ExcludeCreator)? NextTaskRole(Deviation deviation) => deviation.Status switch
    {
        DeviationStatus.Draft => (WorkflowTaskRoles.ProcessAuthority, true),
        DeviationStatus.Submitted or DeviationStatus.PreliminaryReview => (WorkflowTaskRoles.Investigator, false),
        DeviationStatus.ImpactAssessment => (WorkflowTaskRoles.Evaluator, true),
        DeviationStatus.ActionImplementation => deviation.EffectivenessRequired
            ? (WorkflowTaskRoles.Evaluator, true)
            : (WorkflowTaskRoles.Approver, true),
        DeviationStatus.EffectivenessReview => (WorkflowTaskRoles.Approver, true),
        _ => null
    };

    private async Task<DeviationNextAssigneeResponse?> PreviewNextAssigneeAsync(Guid id, CancellationToken ct)
    {
        var routing = await (from deviation in dbContext.Deviations.AsNoTracking()
                             join record in dbContext.QualityRecords.AsNoTracking() on deviation.QualityRecordId equals record.Id
                             where deviation.Id == id
                             select new { Deviation = deviation, record.CreatedByUserId }).SingleAsync(ct);
        if (NextTaskRole(routing.Deviation) is not { } next) return null;
        var assignee = await ResolveAssigneeAsync(routing.Deviation, next.TaskRole,
            next.ExcludeCreator ? routing.CreatedByUserId : null, ct);
        return new DeviationNextAssigneeResponse(next.TaskRole, assignee?.UserId, assignee?.DisplayName,
            assignee?.DepartmentName);
    }

    private static string TaskRoleLabel(string taskRole) => taskRole switch
    {
        WorkflowTaskRoles.ProcessAuthority => "işlem yetkilisi",
        WorkflowTaskRoles.Investigator => "araştırmacı",
        WorkflowTaskRoles.Evaluator => "değerlendiren",
        WorkflowTaskRoles.Approver => "onaylayan",
        _ => taskRole
    };

    private static bool IsApprovalTransition(string transition) => transition.Trim().ToLowerInvariant() is
        "start-preliminary-review" or "complete-quality-assessment" or "complete-effectiveness-review" or "close";

    private static string SignatureMeaning(string transition) => transition.Trim().ToLowerInvariant() switch
    {
        "start-preliminary-review" => "Sapma ön inceleme kararı",
        "complete-quality-assessment" => "Sapma kalite değerlendirme onayı",
        "complete-effectiveness-review" => "Sapma etkinlik doğrulaması",
        "close" => "Sapma nihai kapanış onayı",
        _ => "Sapma iş akışı onayı"
    };

    private async Task<bool> CanCurrentUserPerformAsync(string aggregateType, Guid aggregateId, string taskRole, CancellationToken ct)
    {
        var owners = await dbContext.WorkflowTaskAssignments.AsNoTracking()
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId && x.TaskRole == taskRole && x.Status == WorkflowTaskStatus.Active)
            .Select(x => x.AssignedUserId).ToListAsync(ct);
        if (owners.Contains(currentUser.Id)) return true;
        if (owners.Count == 0) return false;
        var now = timeProvider.GetUtcNow();
        return await dbContext.Delegations.AsNoTracking().AnyAsync(x => owners.Contains(x.DelegatorUserId) && x.DelegateUserId == currentUser.Id && x.RevokedAtUtc == null && x.StartsAtUtc <= now && x.EndsAtUtc >= now && (x.Scope == "ALL" || x.Scope == "M.01"), ct);
    }

    private static string? RequiredTaskRole(string status) => status switch
    {
        nameof(DeviationStatus.Submitted) or nameof(DeviationStatus.PreliminaryReview) or nameof(DeviationStatus.ActionImplementation) => WorkflowTaskRoles.ProcessAuthority,
        nameof(DeviationStatus.Investigation) or nameof(DeviationStatus.ImpactAssessment) => WorkflowTaskRoles.Investigator,
        nameof(DeviationStatus.QualityAssessment) or nameof(DeviationStatus.EffectivenessReview) => WorkflowTaskRoles.Evaluator,
        nameof(DeviationStatus.ClosureApproval) => WorkflowTaskRoles.Approver,
        _ => null
    };

    private async Task<string> CurrentDepartmentNameAsync(CancellationToken ct)
    {
        return (await CurrentDepartmentAsync(ct)).Name;
    }

    private async Task<(Guid Id, string Name)> CurrentDepartmentAsync(CancellationToken ct)
    {
        var department = await (from user in dbContext.Users.AsNoTracking()
                                join item in dbContext.Departments.AsNoTracking() on user.DepartmentId equals item.Id
                                where user.Id == currentUser.Id && user.IsActive && item.IsActive
                                select new { item.Id, item.Name })
            .SingleOrDefaultAsync(ct);

        if (department is null)
            throw new InvalidOperationException("M.01 işlemi için kullanıcıya organizasyonda etkin bir bölüm atanmalıdır.");

        return (department.Id, department.Name);
    }

    private Task AssignNextDeviationTaskAsync(Deviation deviation, Guid creatorId, DateTimeOffset now, CancellationToken ct) => deviation.Status switch
    {
        DeviationStatus.ActionImplementation => AssignRoleAsync(deviation, WorkflowTaskRoles.ProcessAuthority, creatorId, now, ct),
        DeviationStatus.EffectivenessReview => AssignRoleAsync(deviation, WorkflowTaskRoles.Evaluator, creatorId, now, ct),
        DeviationStatus.ClosureApproval => AssignRoleAsync(deviation, WorkflowTaskRoles.Approver, creatorId, now, ct),
        _ => Task.CompletedTask
    };

    private static IReadOnlyList<DeviationTransitionResponse> GetAvailableTransitions(string status) => status switch
    {
        "Submitted" => [new("start-preliminary-review", "Ön incelemeyi tamamla ve araştırmaya gönder", true)],
        // Geriye uyumluluk: eski sürümde ön incelemeye alınmış kayıtlar ilerleyebilsin.
        "PreliminaryReview" => [new("start-investigation", "Araştırmaya gönder", false)],
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
