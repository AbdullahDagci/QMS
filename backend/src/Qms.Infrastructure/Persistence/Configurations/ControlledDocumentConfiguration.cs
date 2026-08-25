using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ControlledDocumentConfiguration : IEntityTypeConfiguration<ControlledDocument>
{
    public void Configure(EntityTypeBuilder<ControlledDocument> builder)
    {
        builder.ToTable("controlled_document", "document"); builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentCode).HasMaxLength(80).IsRequired(); builder.Property(x => x.Title).HasMaxLength(240).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(80).IsRequired(); builder.Property(x => x.Owner).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(120).IsRequired(); builder.Property(x => x.Confidentiality).HasMaxLength(40).IsRequired();
        builder.Property(x => x.WithdrawalReason).HasMaxLength(2000); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(48).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.QualityRecordId).IsUnique(); builder.HasIndex(x => x.DocumentCode).IsUnique(); builder.HasIndex(x => x.SourceChangeControlId);
        builder.HasIndex(x => new { x.Status, x.NextReviewDateUtc });
        builder.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithMany().HasForeignKey(x => x.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Qms.Domain.ChangeControls.ChangeControl>().WithMany().HasForeignKey(x => x.SourceChangeControlId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Revisions).WithOne().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Reviews).WithOne().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.TrainingRequirements).WithOne().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Copies).WithOne().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ReadReceipts).WithOne().HasForeignKey(x => x.ControlledDocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Reviews).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.TrainingRequirements).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Copies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ReadReceipts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(x => x.CurrentRevision);
    }
}
