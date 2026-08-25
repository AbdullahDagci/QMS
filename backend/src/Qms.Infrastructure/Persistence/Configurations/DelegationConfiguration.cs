using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Organization;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("delegation", "organization"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasMaxLength(64).IsRequired(); builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.DelegatorUserId, x.StartsAtUtc, x.EndsAtUtc }); builder.HasIndex(x => new { x.DelegateUserId, x.StartsAtUtc, x.EndsAtUtc });
    }
}
