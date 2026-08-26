using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ChangeControls;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ChangeControlConfiguration : IEntityTypeConfiguration<ChangeControl>
{
    public void Configure(EntityTypeBuilder<ChangeControl> builder)
    {
        builder.ToTable("change_control", "change_control"); builder.HasKey(x => x.Id);
        builder.Property(x => x.ChangeType).HasMaxLength(80).IsRequired(); builder.Property(x => x.Title).HasMaxLength(240).IsRequired();
        builder.Property(x => x.CurrentState).HasMaxLength(4000).IsRequired(); builder.Property(x => x.ProposedState).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(3000).IsRequired(); builder.Property(x => x.Scope).HasMaxLength(3000).IsRequired();
        builder.Property(x => x.Owner).HasMaxLength(160).IsRequired(); builder.Property(x => x.RiskLevel).HasMaxLength(40).IsRequired(); builder.Property(x => x.RiskSummary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RegulatoryImpact).HasMaxLength(80).IsRequired(); builder.Property(x => x.RollbackPlan).HasMaxLength(3000).IsRequired(); builder.Property(x => x.AuthorityApprovalReference).HasMaxLength(500); builder.Property(x => x.PostImplementationResult).HasMaxLength(3000); builder.Property(x => x.ClosureNote).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired(); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.QualityRecordId).IsUnique(); builder.HasIndex(x => x.SourceCapaId); builder.HasIndex(x => new { x.Status, x.TargetDateUtc });
        builder.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Qms.Domain.Capas.Capa>().WithMany().HasForeignKey(x => x.SourceCapaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Qms.Infrastructure.Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasMany(x => x.Assessments).WithOne().HasForeignKey(x => x.ChangeControlId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Actions).WithOne().HasForeignKey(x => x.ChangeControlId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Assessments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
