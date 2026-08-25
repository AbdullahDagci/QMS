using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DocumentReadReceiptConfiguration : IEntityTypeConfiguration<DocumentReadReceipt>
{
    public void Configure(EntityTypeBuilder<DocumentReadReceipt> b) { b.ToTable("read_receipt", "document"); b.HasKey(x => x.Id); b.Property(x => x.UserDisplayName).HasMaxLength(200).IsRequired(); b.Property(x => x.SignatureMeaning).HasMaxLength(500).IsRequired(); b.HasIndex(x => new { x.RevisionId, x.UserId }).IsUnique(); }
}
