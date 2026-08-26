using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Complaints;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> b)
    {
        b.ToTable("complaint", "complaint"); b.HasKey(x => x.Id);
        b.Property(x => x.Channel).HasMaxLength(80).IsRequired(); b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired(); b.Property(x => x.Country).HasMaxLength(100).IsRequired(); b.Property(x => x.Product).HasMaxLength(200).IsRequired(); b.Property(x => x.BatchNumber).HasMaxLength(120); b.Property(x => x.ComplaintType).HasMaxLength(120).IsRequired(); b.Property(x => x.Description).HasMaxLength(6000).IsRequired(); b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20); b.Property(x => x.AttachmentSummary).HasMaxLength(1000); b.Property(x => x.Owner).HasMaxLength(200).IsRequired(); b.Property(x => x.PharmacovigilanceStatus).HasMaxLength(40); b.Property(x => x.ImpactAssessment).HasMaxLength(5000); b.Property(x => x.ConfirmedRootCause).HasMaxLength(4000); b.Property(x => x.ClosureNote).HasMaxLength(2000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.QualityRecordId).IsUnique(); b.HasIndex(x => new { x.Product, x.ComplaintType, x.CreatedAtUtc }); b.HasIndex(x => new { x.Status, x.FinalResponseDueAtUtc });
        b.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Domain.Deviations.Deviation>().WithMany().HasForeignKey(x => x.LinkedDeviationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Domain.Capas.Capa>().WithMany().HasForeignKey(x => x.LinkedCapaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Investigations).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Investigations).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Responses).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade); b.Navigation(x => x.Responses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

