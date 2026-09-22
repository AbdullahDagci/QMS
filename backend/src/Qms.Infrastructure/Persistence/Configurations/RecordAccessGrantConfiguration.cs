using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Organization;
using Qms.Domain.QualityRecords;
using Qms.Domain.Workflows;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class RecordAccessGrantConfiguration : IEntityTypeConfiguration<RecordAccessGrant>
{
    public void Configure(EntityTypeBuilder<RecordAccessGrant> builder)
    {
        builder.ToTable("record_access_grant", "identity");
        builder.HasKey(grant => grant.Id);
        builder.HasIndex(grant => grant.AssignmentId).IsUnique();
        builder.HasIndex(grant => new { grant.UserId, grant.QualityRecordId });
        builder.HasOne<QualityRecord>().WithMany().HasForeignKey(grant => grant.QualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowTaskAssignment>().WithMany().HasForeignKey(grant => grant.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
