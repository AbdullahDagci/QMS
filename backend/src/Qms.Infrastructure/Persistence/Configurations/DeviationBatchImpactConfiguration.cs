using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Deviations;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class DeviationBatchImpactConfiguration : IEntityTypeConfiguration<DeviationBatchImpact>
{
    public void Configure(EntityTypeBuilder<DeviationBatchImpact> builder)
    {
        builder.ToTable("deviation_batch_impact", "deviation");
        builder.HasKey(impact => impact.Id);
        builder.HasOne<Deviation>()
            .WithMany()
            .HasForeignKey(impact => impact.DeviationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(impact => impact.BatchNumber).HasMaxLength(120).IsRequired();
        builder.Property(impact => impact.Disposition).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(impact => impact.Rationale).HasMaxLength(2000).IsRequired();
        builder.Property(impact => impact.AssessedByNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(impact => impact.AssessedByDepartmentSnapshot).HasMaxLength(160).IsRequired();
        builder.HasIndex(impact => new { impact.DeviationId, impact.BatchNumber, impact.AssessedAtUtc });
        builder.HasIndex(impact => new { impact.Disposition, impact.IsLocked });
    }
}
