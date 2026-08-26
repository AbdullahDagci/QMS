using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DocumentLookupDefinitionConfiguration : IEntityTypeConfiguration<DocumentLookupDefinition>
{
    public void Configure(EntityTypeBuilder<DocumentLookupDefinition> b) { b.ToTable("lookup_definition", "document"); b.HasKey(x => x.Id); b.Property(x => x.Category).HasMaxLength(40).IsRequired(); b.Property(x => x.Code).HasMaxLength(64).IsRequired(); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.HasIndex(x => new { x.Category, x.Code }).IsUnique(); b.HasIndex(x => new { x.Category, x.IsActive, x.SortOrder }); }
}
