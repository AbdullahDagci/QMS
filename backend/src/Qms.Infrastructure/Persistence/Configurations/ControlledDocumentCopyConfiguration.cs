using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class ControlledDocumentCopyConfiguration : IEntityTypeConfiguration<ControlledDocumentCopy>
{
    public void Configure(EntityTypeBuilder<ControlledDocumentCopy> b) { b.ToTable("controlled_copy", "document"); b.HasKey(x => x.Id); b.Property(x => x.CopyNumber).HasMaxLength(80).IsRequired(); b.Property(x => x.Recipient).HasMaxLength(200).IsRequired(); b.Property(x => x.Purpose).HasMaxLength(500).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.HasIndex(x => new { x.ControlledDocumentId, x.CopyNumber }).IsUnique(); }
}
