using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.WorkItems;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> b)
    {
        b.ToTable("work_item", "work_tracking"); b.HasKey(x => x.Id);
        b.Property(x => x.SourceModule).HasMaxLength(20); b.Property(x => x.SourceRecordNumber).HasMaxLength(80);
        b.Property(x => x.Category).HasMaxLength(80).IsRequired(); b.Property(x => x.Priority).HasMaxLength(40).IsRequired();
        b.Property(x => x.Title).HasMaxLength(240).IsRequired(); b.Property(x => x.Description).HasMaxLength(6000).IsRequired();
        b.Property(x => x.Owner).HasMaxLength(200).IsRequired(); b.Property(x => x.OwnerDepartment).HasMaxLength(160);
        b.Property(x => x.Verifier).HasMaxLength(200).IsRequired(); b.Property(x => x.CompletionEvidence).HasMaxLength(6000); b.Property(x => x.VerificationNote).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.QualityRecordId).IsUnique(); b.HasIndex(x => new { x.OwnerUserId, x.Status }); b.HasIndex(x => new { x.DueAtUtc, x.Status });
        b.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.VerifierUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkItemLookupDefinitionConfiguration : IEntityTypeConfiguration<WorkItemLookupDefinition>
{
    public void Configure(EntityTypeBuilder<WorkItemLookupDefinition> b)
    {
        b.ToTable("lookup_definition", "work_tracking"); b.HasKey(x => x.Id);
        b.Property(x => x.Category).HasMaxLength(40).IsRequired(); b.Property(x => x.Code).HasMaxLength(80).IsRequired(); b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(x => new { x.Category, x.Code }).IsUnique();
    }
}
