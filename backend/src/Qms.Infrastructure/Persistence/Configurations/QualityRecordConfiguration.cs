using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.QualityRecords;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class QualityRecordConfiguration : IEntityTypeConfiguration<QualityRecord>
{
    public void Configure(EntityTypeBuilder<QualityRecord> builder)
    {
        builder.ToTable("quality_record", "core");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.RecordNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(record => record.RecordNumber).IsUnique();

        builder.Property(record => record.RecordType).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(record => record.Data).HasColumnType("jsonb").IsRequired();
        builder.Property(record => record.Version).IsConcurrencyToken();

        builder.HasIndex(record => new { record.RecordType, record.Status });
        builder.HasIndex(record => record.DepartmentId);
        builder.HasIndex(record => record.CreatedAtUtc);
    }
}
