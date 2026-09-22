using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Files;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ManagedFileConfiguration : IEntityTypeConfiguration<ManagedFile>
{
    public void Configure(EntityTypeBuilder<ManagedFile> builder)
    {
        builder.ToTable("managed_file", "files");
        builder.HasKey(file => file.Id);
        builder.Property(file => file.AggregateType).HasMaxLength(128).IsRequired();
        builder.Property(file => file.Category).HasMaxLength(128).IsRequired();
        builder.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.StoragePath).HasMaxLength(512).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(file => file.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(file => file.IntegrityMac).HasMaxLength(128).IsRequired();
        builder.Property(file => file.UploadedByDisplayName).HasMaxLength(256).IsRequired();
        builder.HasIndex(file => new { file.AggregateType, file.AggregateId, file.UploadedAtUtc });
        builder.HasIndex(file => file.ContentHash);
        builder.HasIndex(file => file.StoragePath).IsUnique();
    }
}
