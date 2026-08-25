using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Organization;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.ToTable("organization_settings", "organization");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Name).HasMaxLength(256).IsRequired();
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(128).IsRequired();
        builder.Property(settings => settings.DefaultCulture).HasMaxLength(32).IsRequired();
    }
}
