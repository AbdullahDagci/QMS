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

namespace Qms.Infrastructure.Persistence;

public sealed class QmsDbContext(DbContextOptions<QmsDbContext> options)
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

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<RecordNumberSequence> RecordNumberSequences => Set<RecordNumberSequence>();

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
