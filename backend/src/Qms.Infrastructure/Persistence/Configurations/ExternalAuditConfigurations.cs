using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ExternalAudits;
using Qms.Infrastructure.Identity;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ExternalAuditConfiguration : IEntityTypeConfiguration<ExternalAudit>
{
    public void Configure(EntityTypeBuilder<ExternalAudit> b)
    {
        b.ToTable("external_audit", "external_audit"); b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(240).IsRequired(); b.Property(x => x.AuditKind).HasMaxLength(100).IsRequired(); b.Property(x => x.AuditorOrganization).HasMaxLength(240).IsRequired(); b.Property(x => x.AuthorityCountry).HasMaxLength(100).IsRequired(); b.Property(x => x.OfficialReference).HasMaxLength(200).IsRequired(); b.Property(x => x.Scope).HasMaxLength(4000).IsRequired(); b.Property(x => x.Site).HasMaxLength(200).IsRequired(); b.Property(x => x.Owner).HasMaxLength(200).IsRequired(); b.Property(x => x.AuthorizedCloser).HasMaxLength(200); b.Property(x => x.ClosureLetterReference).HasMaxLength(300); b.Property(x => x.ClosureEvidence).HasMaxLength(4000); b.Property(x => x.ClosureNote).HasMaxLength(3000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.QualityRecordId).IsUnique(); b.HasIndex(x => new { x.IsGovernmentAuthority, x.Status }); b.HasIndex(x => x.OwnerUserId);
        b.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict); b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorizedCloserUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.DocumentRequests).WithOne().HasForeignKey(x => x.ExternalAuditId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.DocumentRequests).UsePropertyAccessMode(PropertyAccessMode.Field); b.HasMany(x => x.Findings).WithOne().HasForeignKey(x => x.ExternalAuditId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Findings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ExternalAuditDocumentRequestConfiguration : IEntityTypeConfiguration<ExternalAuditDocumentRequest>
{
    public void Configure(EntityTypeBuilder<ExternalAuditDocumentRequest> b) { b.ToTable("document_request", "external_audit"); b.HasKey(x => x.Id); b.Property(x => x.DocumentCode).HasMaxLength(120).IsRequired(); b.Property(x => x.Title).HasMaxLength(300).IsRequired(); b.Property(x => x.Confidentiality).HasMaxLength(80).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); b.HasIndex(x => new { x.ExternalAuditId, x.DocumentCode }); b.HasOne<Qms.Domain.Documents.ControlledDocument>().WithMany().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class ExternalAuditPackageAccessConfiguration : IEntityTypeConfiguration<ExternalAuditPackageAccess>
{
    public void Configure(EntityTypeBuilder<ExternalAuditPackageAccess> b) { b.ToTable("package_access", "external_audit"); b.HasKey(x => x.Id); b.Property(x => x.ExportedBy).HasMaxLength(200).IsRequired(); b.Property(x => x.Recipient).HasMaxLength(300).IsRequired(); b.Property(x => x.Purpose).HasMaxLength(1000).IsRequired(); b.Property(x => x.Evidence).HasMaxLength(2000).IsRequired(); b.HasIndex(x => new { x.ExternalAuditId, x.AccessedAtUtc }); b.HasOne<ExternalAuditDocumentRequest>().WithMany().HasForeignKey(x => x.DocumentRequestId).OnDelete(DeleteBehavior.Restrict); }
}

public sealed class ExternalAuditFindingConfiguration : IEntityTypeConfiguration<ExternalAuditFinding>
{
    public void Configure(EntityTypeBuilder<ExternalAuditFinding> b) { b.ToTable("finding", "external_audit"); b.HasKey(x => x.Id); b.Property(x => x.Number).HasMaxLength(80).IsRequired(); b.Property(x => x.Title).HasMaxLength(240).IsRequired(); b.Property(x => x.Description).HasMaxLength(5000).IsRequired(); b.Property(x => x.OfficialReference).HasMaxLength(500).IsRequired(); b.Property(x => x.Classification).HasConversion<string>().HasMaxLength(30); b.Property(x => x.Owner).HasMaxLength(200).IsRequired(); b.Property(x => x.OfficialResponse).HasMaxLength(8000); b.Property(x => x.Commitment).HasMaxLength(5000); b.Property(x => x.VerificationNote).HasMaxLength(3000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); b.HasIndex(x => x.Number).IsUnique(); b.HasIndex(x => x.OwnerUserId); b.HasOne<Qms.Domain.Capas.Capa>().WithMany().HasForeignKey(x => x.LinkedCapaId).OnDelete(DeleteBehavior.Restrict); b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); }
}
