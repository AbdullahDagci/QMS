using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Deviations;

namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DeviationAssignmentRuleConfiguration : IEntityTypeConfiguration<DeviationAssignmentRule>
{
    public void Configure(EntityTypeBuilder<DeviationAssignmentRule> builder)
    {
        builder.ToTable("assignment_rule", "deviation"); builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskRole).HasMaxLength(64).IsRequired(); builder.Property(x => x.DetectedDepartment).HasMaxLength(160); builder.Property(x => x.DeviationType).HasMaxLength(80);
        builder.HasIndex(x => new { x.TaskRole, x.IsActive, x.Priority }); builder.HasIndex(x => x.AssignedUserId);
    }
}
