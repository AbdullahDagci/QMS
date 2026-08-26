using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Deviations;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class DeviationTypeDefinitionConfiguration : IEntityTypeConfiguration<DeviationTypeDefinition>
{
    public void Configure(EntityTypeBuilder<DeviationTypeDefinition> builder)
    {
        builder.ToTable("deviation_type_definition", "quality");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}
