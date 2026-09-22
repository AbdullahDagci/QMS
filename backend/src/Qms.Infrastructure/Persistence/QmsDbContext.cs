using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Qms.Domain.AuditTrail;
using Qms.Domain.Capas;
using Qms.Domain.ChangeControls;
using Qms.Domain.Deviations;
using Qms.Domain.Documents;
using Qms.Domain.ElectronicSignatures;
using Qms.Domain.Organization;
using Qms.Domain.Notifications;
using Qms.Domain.Identity;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;
using Qms.Domain.Trainings;
using Qms.Domain.Complaints;
using Qms.Domain.InternalAudits;
using Qms.Domain.ExternalAudits;
using Qms.Domain.SupplierAudits;
using Qms.Domain.SpecializedRecords;
using Qms.Domain.WorkItems;
using Qms.Domain.RiskManagement;
using Qms.Domain.MasterBatchRecords;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence.Outbox;
using Qms.Infrastructure.Persistence.Sequences;
using Qms.Domain.Files;
using Qms.Domain.ElectronicForms;
using Qms.Infrastructure.Integrity;
using System.Text.Json;

namespace Qms.Infrastructure.Persistence;

public sealed class QmsDbContext(DbContextOptions<QmsDbContext> options, RecordIntegrityService? integrity = null)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<QualityRecord> QualityRecords => Set<QualityRecord>();

    public DbSet<Deviation> Deviations => Set<Deviation>();

    public DbSet<DeviationInvestigation> DeviationInvestigations => Set<DeviationInvestigation>();

    public DbSet<DeviationBatchImpact> DeviationBatchImpacts => Set<DeviationBatchImpact>();

    public DbSet<DeviationTypeDefinition> DeviationTypeDefinitions => Set<DeviationTypeDefinition>();
    public DbSet<DeviationAssignmentRule> DeviationAssignmentRules => Set<DeviationAssignmentRule>();

    public DbSet<Capa> Capas => Set<Capa>();

    public DbSet<CapaAction> CapaActions => Set<CapaAction>();

    public DbSet<ChangeControl> ChangeControls => Set<ChangeControl>();
    public DbSet<ChangeAssessment> ChangeAssessments => Set<ChangeAssessment>();
    public DbSet<ChangeImplementationAction> ChangeImplementationActions => Set<ChangeImplementationAction>();
    public DbSet<ChangeLookupDefinition> ChangeLookupDefinitions => Set<ChangeLookupDefinition>();
    public DbSet<ControlledDocument> ControlledDocuments => Set<ControlledDocument>();
    public DbSet<DocumentLookupDefinition> DocumentLookupDefinitions => Set<DocumentLookupDefinition>();
    public DbSet<DocumentRevision> DocumentRevisions => Set<DocumentRevision>();
    public DbSet<DocumentReview> DocumentReviews => Set<DocumentReview>();
    public DbSet<DocumentTrainingRequirement> DocumentTrainingRequirements => Set<DocumentTrainingRequirement>();
    public DbSet<ControlledDocumentCopy> ControlledDocumentCopies => Set<ControlledDocumentCopy>();
    public DbSet<DocumentReadReceipt> DocumentReadReceipts => Set<DocumentReadReceipt>();
    public DbSet<TrainingMatrixRule> TrainingMatrixRules => Set<TrainingMatrixRule>();
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
    public DbSet<TrainingAssessmentAttempt> TrainingAssessmentAttempts => Set<TrainingAssessmentAttempt>();
    public DbSet<TrainingLookupDefinition> TrainingLookupDefinitions => Set<TrainingLookupDefinition>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintInvestigation> ComplaintInvestigations => Set<ComplaintInvestigation>();
    public DbSet<ComplaintResponse> ComplaintResponses => Set<ComplaintResponse>();
    public DbSet<ComplaintLookupDefinition> ComplaintLookupDefinitions => Set<ComplaintLookupDefinition>();
    public DbSet<InternalAudit> InternalAudits => Set<InternalAudit>();
    public DbSet<InternalAuditLookupDefinition> InternalAuditLookupDefinitions => Set<InternalAuditLookupDefinition>();
    public DbSet<AuditChecklistItem> AuditChecklistItems => Set<AuditChecklistItem>();
    public DbSet<AuditFinding> AuditFindings => Set<AuditFinding>();
    public DbSet<ExternalAudit> ExternalAudits => Set<ExternalAudit>();
    public DbSet<ExternalAuditLookupDefinition> ExternalAuditLookupDefinitions => Set<ExternalAuditLookupDefinition>();
    public DbSet<SupplierAuditLookupDefinition> SupplierAuditLookupDefinitions => Set<SupplierAuditLookupDefinition>();
    public DbSet<SpecializedRecord> SpecializedRecords => Set<SpecializedRecord>();
    public DbSet<SpecializedLookupDefinition> SpecializedLookupDefinitions => Set<SpecializedLookupDefinition>();
    public DbSet<ExternalAuditDocumentRequest> ExternalAuditDocumentRequests => Set<ExternalAuditDocumentRequest>();
    public DbSet<ExternalAuditPackageAccess> ExternalAuditPackageAccesses => Set<ExternalAuditPackageAccess>();
    public DbSet<ExternalAuditFinding> ExternalAuditFindings => Set<ExternalAuditFinding>();
    public DbSet<SupplierAudit> SupplierAudits => Set<SupplierAudit>();
    public DbSet<SupplierAuditChecklistItem> SupplierAuditChecklistItems => Set<SupplierAuditChecklistItem>();
    public DbSet<SupplierAuditFinding> SupplierAuditFindings => Set<SupplierAuditFinding>();
    public DbSet<SupplierAuditInvitation> SupplierAuditInvitations => Set<SupplierAuditInvitation>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemLookupDefinition> WorkItemLookupDefinitions => Set<WorkItemLookupDefinition>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<RiskItem> RiskItems => Set<RiskItem>();
    public DbSet<RiskLookupDefinition> RiskLookupDefinitions => Set<RiskLookupDefinition>();
    public DbSet<MasterBatchRecord> MasterBatchRecords => Set<MasterBatchRecord>();
    public DbSet<MasterBatchStep> MasterBatchSteps => Set<MasterBatchStep>();
    public DbSet<MbrLookupDefinition> MbrLookupDefinitions => Set<MbrLookupDefinition>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<ElectronicSignature> ElectronicSignatures => Set<ElectronicSignature>();

    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<UserPosition> UserPositions => Set<UserPosition>();
    public DbSet<Delegation> Delegations => Set<Delegation>();
    public DbSet<WorkflowTaskAssignment> WorkflowTaskAssignments => Set<WorkflowTaskAssignment>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RecordAccessGrant> RecordAccessGrants => Set<RecordAccessGrant>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<RecordNumberSequence> RecordNumberSequences => Set<RecordNumberSequence>();
    public DbSet<ManagedFile> ManagedFiles => Set<ManagedFile>();
    public DbSet<ElectronicFormDefinition> ElectronicFormDefinitions => Set<ElectronicFormDefinition>();
    public DbSet<ElectronicFormVersion> ElectronicFormVersions => Set<ElectronicFormVersion>();
    public DbSet<ElectronicFormOutputTemplate> ElectronicFormOutputTemplates => Set<ElectronicFormOutputTemplate>();
    public DbSet<ElectronicFormRecord> ElectronicFormRecords => Set<ElectronicFormRecord>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var ownsTransaction = HasPendingAuditEvents() && Database.CurrentTransaction is null
            && Database.ProviderName?.StartsWith("Npgsql", StringComparison.Ordinal) == true;
        await using var transaction = ownsTransaction
            ? await Database.BeginTransactionAsync(cancellationToken) : null;
        GuardImmutableRecords();
        EnqueueNotifications();
        await EnqueueRecordAccessGrantsAsync(cancellationToken);
        await SealAuditEventsAsync(cancellationToken);
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var ownsTransaction = HasPendingAuditEvents() && Database.CurrentTransaction is null
            && Database.ProviderName?.StartsWith("Npgsql", StringComparison.Ordinal) == true;
        using var transaction = ownsTransaction ? Database.BeginTransaction() : null;
        GuardImmutableRecords();
        EnqueueNotifications();
        EnqueueRecordAccessGrants();
        SealAuditEvents();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        transaction?.Commit();
        return result;
    }

    private bool HasPendingAuditEvents() => ChangeTracker.Entries<AuditEvent>()
        .Any(entry => entry.State == EntityState.Added);

    private void GuardImmutableRecords()
    {
        var mutation = ChangeTracker.Entries().FirstOrDefault(entry =>
            entry.State is EntityState.Modified or EntityState.Deleted
            && entry.Entity is AuditEvent or ElectronicSignature or ManagedFile);
        if (mutation is not null)
            throw new InvalidOperationException($"{mutation.Metadata.ClrType.Name} kayıtları değiştirilemez veya silinemez.");
    }

    private void EnqueueNotifications()
    {
        var notifications = ChangeTracker.Entries<UserNotification>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();
        var existing = ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.Entity.Id).ToHashSet();
        foreach (var notification in notifications.Where(item => !existing.Contains(item.Id)))
            OutboxMessages.Add(OutboxMessage.Create(notification.Id, "notification.created",
                JsonSerializer.SerializeToDocument(new
                {
                    notification.UserId,
                    notification.ModuleCode,
                    notification.Title,
                    notification.Message,
                    notification.Link
                }), notification.CreatedAtUtc));
    }

    private async Task EnqueueRecordAccessGrantsAsync(CancellationToken cancellationToken)
    {
        var assignments = ChangeTracker.Entries<WorkflowTaskAssignment>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();
        var tracked = ChangeTracker.Entries<RecordAccessGrant>().Select(entry => entry.Entity.AssignmentId).ToHashSet();
        foreach (var assignment in assignments.Where(item => !tracked.Contains(item.Id)))
        {
            if (await RecordAccessGrants.AsNoTracking().AnyAsync(item => item.AssignmentId == assignment.Id, cancellationToken)) continue;
            var qualityRecordId = await ResolveQualityRecordIdAsync(assignment.AggregateType,
                assignment.AggregateId, cancellationToken);
            if (qualityRecordId.HasValue)
                RecordAccessGrants.Add(RecordAccessGrant.Create(qualityRecordId.Value,
                    assignment.AssignedUserId, assignment.Id, assignment.AssignedAtUtc));
        }
    }

    private void EnqueueRecordAccessGrants()
    {
        var assignments = ChangeTracker.Entries<WorkflowTaskAssignment>()
            .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToList();
        var tracked = ChangeTracker.Entries<RecordAccessGrant>().Select(entry => entry.Entity.AssignmentId).ToHashSet();
        foreach (var assignment in assignments.Where(item => !tracked.Contains(item.Id)))
        {
            if (RecordAccessGrants.AsNoTracking().Any(item => item.AssignmentId == assignment.Id)) continue;
            var qualityRecordId = ResolveQualityRecordId(assignment.AggregateType, assignment.AggregateId);
            if (qualityRecordId.HasValue)
                RecordAccessGrants.Add(RecordAccessGrant.Create(qualityRecordId.Value,
                    assignment.AssignedUserId, assignment.Id, assignment.AssignedAtUtc));
        }
    }

    internal async Task<Guid?> ResolveQualityRecordIdAsync(string aggregateType, Guid aggregateId,
        CancellationToken cancellationToken)
    {
        var tracked = ResolveTrackedQualityRecordId(aggregateType, aggregateId);
        if (tracked.HasValue) return tracked;
        return aggregateType switch
        {
            "Deviation" => await Deviations.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "Capa" => await Capas.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "ChangeControl" => await ChangeControls.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "Document" => await ControlledDocuments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "Training" => await TrainingAssignments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "Complaint" => await Complaints.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "InternalAudit" => await InternalAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "ExternalAudit" => await ExternalAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "SupplierAudit" => await SupplierAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "WorkItem" => await WorkItems.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "RiskAssessment" => await RiskAssessments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "MasterBatchRecord" => await MasterBatchRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "SpecializedRecord" => await SpecializedRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "ElectronicFormRecord" => await ElectronicFormRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            "ElectronicFormDefinition" => await ElectronicFormDefinitions.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefaultAsync(cancellationToken),
            _ => null
        };
    }

    private Guid? ResolveQualityRecordId(string aggregateType, Guid aggregateId)
    {
        var tracked = ResolveTrackedQualityRecordId(aggregateType, aggregateId);
        if (tracked.HasValue) return tracked;
        return aggregateType switch
        {
            "Deviation" => Deviations.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "Capa" => Capas.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "ChangeControl" => ChangeControls.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "Document" => ControlledDocuments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "Training" => TrainingAssignments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "Complaint" => Complaints.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "InternalAudit" => InternalAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "ExternalAudit" => ExternalAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "SupplierAudit" => SupplierAudits.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "WorkItem" => WorkItems.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "RiskAssessment" => RiskAssessments.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "MasterBatchRecord" => MasterBatchRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "SpecializedRecord" => SpecializedRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "ElectronicFormRecord" => ElectronicFormRecords.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            "ElectronicFormDefinition" => ElectronicFormDefinitions.Where(x => x.Id == aggregateId).Select(x => (Guid?)x.QualityRecordId).SingleOrDefault(),
            _ => null
        };
    }

    private Guid? ResolveTrackedQualityRecordId(string aggregateType, Guid aggregateId) => aggregateType switch
    {
        "Deviation" => Deviations.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "Capa" => Capas.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "ChangeControl" => ChangeControls.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "Document" => ControlledDocuments.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "Training" => TrainingAssignments.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "Complaint" => Complaints.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "InternalAudit" => InternalAudits.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "ExternalAudit" => ExternalAudits.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "SupplierAudit" => SupplierAudits.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "WorkItem" => WorkItems.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "RiskAssessment" => RiskAssessments.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "MasterBatchRecord" => MasterBatchRecords.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "SpecializedRecord" => SpecializedRecords.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "ElectronicFormRecord" => ElectronicFormRecords.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        "ElectronicFormDefinition" => ElectronicFormDefinitions.Local.FirstOrDefault(x => x.Id == aggregateId)?.QualityRecordId,
        _ => null
    };

    private async Task SealAuditEventsAsync(CancellationToken cancellationToken)
    {
        var pending = ChangeTracker.Entries<AuditEvent>().Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity).OrderBy(entry => entry.OccurredAtUtc).ThenBy(entry => entry.Id).ToList();
        if (pending.Count > 0 && Database.ProviderName?.StartsWith("Npgsql", StringComparison.Ordinal) == true)
            foreach (var key in pending.Select(item => $"{item.AggregateType}|{item.AggregateId:N}")
                         .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                await Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        var previous = new Dictionary<(string Type, Guid Id), string>();
        foreach (var auditEvent in pending)
        {
            var key = (Type: auditEvent.AggregateType, Id: auditEvent.AggregateId);
            if (!previous.TryGetValue(key, out var previousHash))
                previousHash = await AuditEvents.AsNoTracking()
                    .Where(item => item.AggregateType == key.Type && item.AggregateId == key.Id)
                    .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.Id)
                    .Select(item => item.IntegrityHash).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            var hash = RecordIntegrityService.AuditHash(auditEvent, previousHash);
            auditEvent.Seal(previousHash, hash, integrity?.Mac(hash) ?? string.Empty);
            previous[key] = hash;
        }
    }

    private void SealAuditEvents()
    {
        var pending = ChangeTracker.Entries<AuditEvent>().Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity).OrderBy(entry => entry.OccurredAtUtc).ThenBy(entry => entry.Id).ToList();
        if (pending.Count > 0 && Database.ProviderName?.StartsWith("Npgsql", StringComparison.Ordinal) == true)
            foreach (var key in pending.Select(item => $"{item.AggregateType}|{item.AggregateId:N}")
                         .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                Database.ExecuteSqlInterpolated($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
        var previous = new Dictionary<(string Type, Guid Id), string>();
        foreach (var auditEvent in pending)
        {
            var key = (Type: auditEvent.AggregateType, Id: auditEvent.AggregateId);
            if (!previous.TryGetValue(key, out var previousHash))
                previousHash = AuditEvents.AsNoTracking()
                    .Where(item => item.AggregateType == key.Type && item.AggregateId == key.Id)
                    .OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.Id)
                    .Select(item => item.IntegrityHash).FirstOrDefault() ?? string.Empty;
            var hash = RecordIntegrityService.AuditHash(auditEvent, previousHash);
            auditEvent.Seal(previousHash, hash, integrity?.Mac(hash) ?? string.Empty);
            previous[key] = hash;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(QmsDbContext).Assembly);

        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("user", "identity");
            user.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            user.Property(item => item.ProfileKey).HasMaxLength(64);
            user.HasIndex(item => item.ProfileKey).IsUnique().HasFilter("\"ProfileKey\" IS NOT NULL");
            user.HasIndex(item => item.DepartmentId);
        });
        builder.Entity<IdentityRole<Guid>>().ToTable("role", "identity");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_role", "identity");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claim", "identity");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_login", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claim", "identity");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_token", "identity");
    }
}
