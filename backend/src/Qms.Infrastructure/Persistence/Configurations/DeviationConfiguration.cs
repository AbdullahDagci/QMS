using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Deviations;
using Qms.Domain.QualityRecords;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class DeviationConfiguration : IEntityTypeConfiguration<Deviation>
{
    public void Configure(EntityTypeBuilder<Deviation> builder)
    {
        builder.ToTable("deviation", "deviation");
        builder.HasKey(deviation => deviation.Id);

        builder.HasOne<QualityRecord>()
            .WithOne()
            .HasForeignKey<Deviation>(deviation => deviation.QualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(deviation => deviation.QualityRecordId).IsUnique();
        builder.HasIndex(deviation => new { deviation.Status, deviation.TargetDateUtc });
        builder.HasIndex(deviation => new { deviation.Classification, deviation.CreatedAtUtc });

        builder.Property(deviation => deviation.Title).HasMaxLength(200).IsRequired();
        builder.Property(deviation => deviation.Description).HasMaxLength(4000).IsRequired();
        builder.Property(deviation => deviation.ExpectedState).HasMaxLength(2000).IsRequired();
        builder.Property(deviation => deviation.ImmediateAction).HasMaxLength(2000).IsRequired();
        builder.Property(deviation => deviation.DeviationType).HasMaxLength(80).IsRequired();
        builder.Property(deviation => deviation.DetectedDepartment).HasMaxLength(160).IsRequired();
        builder.Property(deviation => deviation.ProcessStage).HasMaxLength(160).IsRequired();
        builder.Property(deviation => deviation.PreliminaryReviewNote).HasMaxLength(2000);
        builder.Property(deviation => deviation.QualityAssessmentNote).HasMaxLength(4000);
        builder.Property(deviation => deviation.EffectivenessAssessmentNote).HasMaxLength(4000);
        builder.Property(deviation => deviation.ClosureJustification).HasMaxLength(2000);
        builder.Property(deviation => deviation.Classification).HasConversion<string>().HasMaxLength(32);
        builder.Property(deviation => deviation.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(deviation => deviation.Version).IsConcurrencyToken();
    }
}
