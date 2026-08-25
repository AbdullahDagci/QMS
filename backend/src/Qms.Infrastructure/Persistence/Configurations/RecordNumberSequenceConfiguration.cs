using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Infrastructure.Persistence.Sequences;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class RecordNumberSequenceConfiguration : IEntityTypeConfiguration<RecordNumberSequence>
{
    public void Configure(EntityTypeBuilder<RecordNumberSequence> builder)
    {
        builder.ToTable("record_number_sequence", "core");
        builder.HasKey(sequence => new { sequence.RecordType, sequence.CalendarYear });
        builder.Property(sequence => sequence.RecordType).HasMaxLength(64);
    }
}
