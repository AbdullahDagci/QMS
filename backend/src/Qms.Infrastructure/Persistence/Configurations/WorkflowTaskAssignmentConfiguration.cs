using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Workflows;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTaskAssignmentConfiguration : IEntityTypeConfiguration<WorkflowTaskAssignment>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskAssignment> builder)
    {
        builder.ToTable("task_assignment", "workflow"); builder.HasKey(x => x.Id);
        builder.Property(x => x.AggregateType).HasMaxLength(64).IsRequired(); builder.Property(x => x.TaskRole).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AssignedUserNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.AssignedDepartmentNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.HasIndex(x => new { x.AggregateType, x.AggregateId, x.Status }); builder.HasIndex(x => new { x.AssignedUserId, x.Status, x.DueAtUtc });
        builder.HasIndex(x => new { x.Status, x.DueAtUtc, x.LastEscalatedAtUtc });
    }
}
