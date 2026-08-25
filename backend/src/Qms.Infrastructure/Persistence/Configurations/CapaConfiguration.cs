using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Capas;
using Qms.Domain.Deviations;
using Qms.Domain.QualityRecords;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class CapaConfiguration : IEntityTypeConfiguration<Capa>
{
    public void Configure(EntityTypeBuilder<Capa> builder)
    {
        builder.ToTable("capa", "capa");
        builder.HasKey(x => x.Id);
        builder.HasOne<QualityRecord>().WithOne().HasForeignKey<Capa>(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Deviation>().WithMany().HasForeignKey(x => x.SourceDeviationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Actions).WithOne().HasForeignKey(x => x.CapaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => x.QualityRecordId).IsUnique();
        builder.HasIndex(x => x.SourceDeviationId).IsUnique().HasFilter("\"SourceDeviationId\" IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.TargetDateUtc });
        builder.Property(x => x.SourceType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RootCause).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ImmediateActions).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Owner).HasMaxLength(160).IsRequired();
        builder.Property(x => x.EffectivenessMethod).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.EffectivenessSample).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SuccessCriteria).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.EffectivenessEvaluator).HasMaxLength(160).IsRequired();
        builder.Property(x => x.EffectivenessResult).HasMaxLength(4000);
        builder.Property(x => x.ClosureNote).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
