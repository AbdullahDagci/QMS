using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.SupplierAudits;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class SupplierAuditConfiguration : IEntityTypeConfiguration<SupplierAudit>
{
    public void Configure(EntityTypeBuilder<SupplierAudit> b)
    {
        b.ToTable("supplier_audit", "supplier_audit"); b.HasKey(x => x.Id);
        b.Property(x => x.SupplierCode).HasMaxLength(80).IsRequired(); b.Property(x => x.SupplierName).HasMaxLength(240).IsRequired(); b.Property(x => x.SupplierScope).HasMaxLength(500).IsRequired(); b.Property(x => x.MaterialOrService).HasMaxLength(300).IsRequired(); b.Property(x => x.Country).HasMaxLength(100).IsRequired(); b.Property(x => x.Criticality).HasMaxLength(40).IsRequired(); b.Property(x => x.PastPerformanceScore).HasPrecision(5, 2); b.Property(x => x.RiskBand).HasMaxLength(40).IsRequired(); b.Property(x => x.Scope).HasMaxLength(4000).IsRequired(); b.Property(x => x.Site).HasMaxLength(240).IsRequired(); b.Property(x => x.LeadAuditor).HasMaxLength(200).IsRequired(); b.Property(x => x.LeadAuditorDepartment).HasMaxLength(160).IsRequired(); b.Property(x => x.PurchasingOwner).HasMaxLength(200).IsRequired(); b.Property(x => x.Verifier).HasMaxLength(200).IsRequired(); b.Property(x => x.QualityApprover).HasMaxLength(200).IsRequired(); b.Property(x => x.ChecklistVersion).HasMaxLength(80).IsRequired(); b.Property(x => x.QualificationStatus).HasConversion<string>().HasMaxLength(40); b.Property(x => x.ResultRationale).HasMaxLength(4000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.QualityRecordId).IsUnique(); b.HasIndex(x => new { x.SupplierCode, x.SupplierScope }); b.HasIndex(x => new { x.RiskBand, x.Status });
        b.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.LeadAuditorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.PurchasingOwnerUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.VerifierUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.QualityApproverUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Checklist).WithOne().HasForeignKey(x => x.SupplierAuditId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Checklist).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Findings).WithOne().HasForeignKey(x => x.SupplierAuditId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Findings).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Invitations).WithOne().HasForeignKey(x => x.SupplierAuditId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Invitations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SupplierAuditChecklistConfiguration : IEntityTypeConfiguration<SupplierAuditChecklistItem>
{
    public void Configure(EntityTypeBuilder<SupplierAuditChecklistItem> b) { b.ToTable("checklist_item", "supplier_audit"); b.HasKey(x => x.Id); b.Property(x => x.Category).HasMaxLength(120).IsRequired(); b.Property(x => x.Question).HasMaxLength(1200).IsRequired(); b.Property(x => x.Reference).HasMaxLength(400).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); b.Property(x => x.Evidence).HasMaxLength(4000); b.Property(x => x.Note).HasMaxLength(3000); b.HasIndex(x => new { x.SupplierAuditId, x.Order }).IsUnique(); }
}

public sealed class SupplierAuditFindingConfiguration : IEntityTypeConfiguration<SupplierAuditFinding>
{
    public void Configure(EntityTypeBuilder<SupplierAuditFinding> b) { b.ToTable("finding", "supplier_audit"); b.HasKey(x => x.Id); b.Property(x => x.Number).HasMaxLength(80).IsRequired(); b.Property(x => x.Title).HasMaxLength(240).IsRequired(); b.Property(x => x.Description).HasMaxLength(6000).IsRequired(); b.Property(x => x.RequirementReference).HasMaxLength(500).IsRequired(); b.Property(x => x.Classification).HasConversion<string>().HasMaxLength(30); b.Property(x => x.Owner).HasMaxLength(200).IsRequired(); b.Property(x => x.SupplierResponse).HasMaxLength(8000); b.Property(x => x.Commitment).HasMaxLength(5000); b.Property(x => x.Evidence).HasMaxLength(5000); b.Property(x => x.VerificationNote).HasMaxLength(3000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); b.HasIndex(x => x.Number).IsUnique(); b.HasOne<Qms.Domain.Capas.Capa>().WithMany().HasForeignKey(x => x.LinkedCapaId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class SupplierAuditInvitationConfiguration : IEntityTypeConfiguration<SupplierAuditInvitation>
{
    public void Configure(EntityTypeBuilder<SupplierAuditInvitation> b) { b.ToTable("invitation", "supplier_audit"); b.HasKey(x => x.Id); b.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired(); b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); b.HasIndex(x => x.TokenHash).IsUnique(); b.HasIndex(x => new { x.SupplierAuditId, x.ExpiresAtUtc }); }
}
