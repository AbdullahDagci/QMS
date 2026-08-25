using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ChangeControls;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ChangeImplementationActionConfiguration : IEntityTypeConfiguration<ChangeImplementationAction>
{
    public void Configure(EntityTypeBuilder<ChangeImplementationAction> builder)
    {
        builder.ToTable("implementation_action", "change_control"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(80).IsRequired(); builder.Property(x => x.Description).HasMaxLength(2000).IsRequired(); builder.Property(x => x.Owner).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); builder.Property(x => x.CompletionEvidence).HasMaxLength(3000); builder.Property(x => x.VerificationNote).HasMaxLength(2000);
        builder.HasIndex(x => new { x.ChangeControlId, x.Status });
    }
}
