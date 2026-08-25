using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DocumentRevisionConfiguration : IEntityTypeConfiguration<DocumentRevision>
{
    public void Configure(EntityTypeBuilder<DocumentRevision> b) { b.ToTable("revision", "document"); b.HasKey(x => x.Id); b.Property(x => x.Content).HasMaxLength(50000).IsRequired(); b.Property(x => x.ChangeSummary).HasMaxLength(2000).IsRequired(); b.Property(x => x.PreparedBy).HasMaxLength(160).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Ignore(x => x.VersionLabel); b.HasIndex(x => new { x.ControlledDocumentId, x.MajorVersion, x.MinorVersion }).IsUnique(); }
}
