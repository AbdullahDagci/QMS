using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Deviations;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class DeviationInvestigationConfiguration : IEntityTypeConfiguration<DeviationInvestigation>
{
    public void Configure(EntityTypeBuilder<DeviationInvestigation> builder)
    {
        builder.ToTable("deviation_investigation", "deviation");
        builder.HasKey(investigation => investigation.Id);
        builder.HasOne<Deviation>()
            .WithMany()
            .HasForeignKey(investigation => investigation.DeviationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(investigation => investigation.Method).HasMaxLength(120).IsRequired();
        builder.Property(investigation => investigation.RootCauseCategory).HasMaxLength(120).IsRequired();
        builder.Property(investigation => investigation.RootCauseDescription).HasMaxLength(4000).IsRequired();
        builder.Property(investigation => investigation.Conclusion).HasMaxLength(4000).IsRequired();
        builder.HasIndex(investigation => new { investigation.DeviationId, investigation.CompletedAtUtc });
    }
}
