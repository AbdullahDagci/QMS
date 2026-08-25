using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Capas;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class CapaActionConfiguration : IEntityTypeConfiguration<CapaAction>
{
    public void Configure(EntityTypeBuilder<CapaAction> builder)
    {
        builder.ToTable("capa_action", "capa");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CapaId, x.TargetDateUtc });
        builder.Property(x => x.ActionType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Owner).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CompletionEvidence).HasMaxLength(4000);
        builder.Property(x => x.VerificationNote).HasMaxLength(2000);
    }
}
