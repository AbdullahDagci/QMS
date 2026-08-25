using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DocumentReviewConfiguration : IEntityTypeConfiguration<DocumentReview>
{
    public void Configure(EntityTypeBuilder<DocumentReview> b) { b.ToTable("review", "document"); b.HasKey(x => x.Id); b.Property(x => x.Department).HasMaxLength(120).IsRequired(); b.Property(x => x.Reviewer).HasMaxLength(160).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.Comment).HasMaxLength(2000); b.HasIndex(x => new { x.RevisionId, x.Department }).IsUnique(); }
}
