using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ElectronicSignatures;
using Qms.Domain.QualityRecords;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ElectronicSignatureConfiguration : IEntityTypeConfiguration<ElectronicSignature>
{
    public void Configure(EntityTypeBuilder<ElectronicSignature> builder)
    {
        builder.ToTable("electronic_signature", "core");
        builder.HasKey(signature => signature.Id);

        builder.Property(signature => signature.SignerDisplayNameSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(signature => signature.Meaning).HasMaxLength(128).IsRequired();
        builder.Property(signature => signature.Comment).HasMaxLength(2000);
        builder.Property(signature => signature.ContentHash).HasMaxLength(128).IsRequired();

        builder.HasOne<QualityRecord>()
            .WithMany()
            .HasForeignKey(signature => signature.QualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(signature => new { signature.QualityRecordId, signature.RecordVersion });
        builder.HasIndex(signature => signature.SignedAtUtc);
    }
}
