using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.SupplierAudits;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class SupplierAuditLookupDefinitionConfiguration : IEntityTypeConfiguration<SupplierAuditLookupDefinition>
{
    public void Configure(EntityTypeBuilder<SupplierAuditLookupDefinition> b)
    {
        b.ToTable("lookup_definition", "supplier_audit"); b.HasKey(x => x.Id);
        b.Property(x => x.Category).HasMaxLength(40).IsRequired(); b.Property(x => x.Code).HasMaxLength(64).IsRequired(); b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(x => new { x.Category, x.Code }).IsUnique(); b.HasIndex(x => new { x.Category, x.IsActive, x.SortOrder });
    }
}
