using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.ElectronicForms;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class ElectronicFormDefinitionConfiguration : IEntityTypeConfiguration<ElectronicFormDefinition>
{
    public void Configure(EntityTypeBuilder<ElectronicFormDefinition> builder)
    {
        builder.ToTable("form_definition", "forms");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.QualityRecordId).IsUnique();
        builder.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithOne()
            .HasForeignKey<ElectronicFormDefinition>(item => item.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(item => item.Code).IsUnique();
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Category).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.CreatedByDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.IsActive, item.Category });
        builder.HasIndex(item => item.CurrentPublishedVersionId);
        builder.HasOne<ElectronicFormVersion>().WithMany()
            .HasForeignKey(item => item.CurrentPublishedVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ElectronicFormVersionConfiguration : IEntityTypeConfiguration<ElectronicFormVersion>
{
    public void Configure(EntityTypeBuilder<ElectronicFormVersion> builder)
    {
        builder.ToTable("form_version", "forms");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.FormDefinitionId, item.VersionNumber }).IsUnique();
        builder.HasOne<ElectronicFormDefinition>().WithMany().HasForeignKey(item => item.FormDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.Schema).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.ChangeSummary).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.WorkflowType).HasMaxLength(64).IsRequired();
        builder.Property(item => item.CreatedByDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.PublishedByDisplayName).HasMaxLength(200);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.FormDefinitionId, item.Status });
    }
}

public sealed class ElectronicFormOutputTemplateConfiguration : IEntityTypeConfiguration<ElectronicFormOutputTemplate>
{
    public void Configure(EntityTypeBuilder<ElectronicFormOutputTemplate> builder)
    {
        builder.ToTable("output_template", "forms");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.FormVersionId).IsUnique();
        builder.HasOne<ElectronicFormVersion>().WithMany().HasForeignKey(item => item.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.TemplateType).HasMaxLength(32).IsRequired();
        builder.Property(item => item.Configuration).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
    }
}

public sealed class ElectronicFormRecordConfiguration : IEntityTypeConfiguration<ElectronicFormRecord>
{
    public void Configure(EntityTypeBuilder<ElectronicFormRecord> builder)
    {
        builder.ToTable("form_record", "forms");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.QualityRecordId).IsUnique();
        builder.HasOne<Qms.Domain.QualityRecords.QualityRecord>().WithOne()
            .HasForeignKey<ElectronicFormRecord>(item => item.QualityRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ElectronicFormDefinition>().WithMany().HasForeignKey(item => item.FormDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ElectronicFormVersion>().WithMany().HasForeignKey(item => item.FormVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ElectronicFormOutputTemplate>().WithMany().HasForeignKey(item => item.OutputTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.FormCodeSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(item => item.FormNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Data).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.CreatedByDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.FormDefinitionId, item.Status });
        builder.HasIndex(item => item.CreatedAtUtc);
    }
}
