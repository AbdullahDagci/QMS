using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ChangeControls;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ChangeAssessmentConfiguration : IEntityTypeConfiguration<ChangeAssessment>
{
    public void Configure(EntityTypeBuilder<ChangeAssessment> builder)
    {
        builder.ToTable("assessment", "change_control"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Department).HasMaxLength(120).IsRequired(); builder.Property(x => x.Reviewer).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); builder.Property(x => x.ImpactSummary).HasMaxLength(2000); builder.Property(x => x.RequiredActions).HasMaxLength(2000);
        builder.HasIndex(x => new { x.ChangeControlId, x.Department }).IsUnique();
    }
}
