using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ChangeControls;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ChangeLookupDefinitionConfiguration : IEntityTypeConfiguration<ChangeLookupDefinition>
{
    public void Configure(EntityTypeBuilder<ChangeLookupDefinition> builder)
    {
        builder.ToTable("change_lookup_definition", "quality"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(40).IsRequired(); builder.Property(x => x.Code).HasMaxLength(64).IsRequired(); builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.Category, x.Code }).IsUnique(); builder.HasIndex(x => new { x.Category, x.IsActive, x.SortOrder });
    }
}
