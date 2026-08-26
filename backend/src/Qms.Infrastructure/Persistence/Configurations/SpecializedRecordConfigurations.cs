using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.SpecializedRecords;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class SpecializedRecordConfiguration : IEntityTypeConfiguration<SpecializedRecord>
{
    public void Configure(EntityTypeBuilder<SpecializedRecord> b)
    {
        b.ToTable("record", "specialized");
        b.HasKey(x => x.Id);
        b.Property(x => x.ModuleCode).HasMaxLength(10);
        b.Property(x => x.Title).HasMaxLength(500);
        b.Property(x => x.TypeCode).HasMaxLength(80);
        b.Property(x => x.TypeName).HasMaxLength(200);
        b.Property(x => x.SubjectCode).HasMaxLength(80);
        b.Property(x => x.SubjectName).HasMaxLength(240);
        b.Property(x => x.ScopeCode).HasMaxLength(80);
        b.Property(x => x.ScopeName).HasMaxLength(240);
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.Description).HasMaxLength(6000);
        b.Property(x => x.StructuredDataJson).HasColumnType("jsonb");
        b.Property(x => x.Owner).HasMaxLength(200);
        b.Property(x => x.Reviewer).HasMaxLength(200);
        b.Property(x => x.Approver).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.QualityRecordId).IsUnique();
        b.HasIndex(x => new
        {
            x.ModuleCode,
            x.Status,
            x.DueAtUtc,
        });
        b.HasOne<Qms.Domain.QualityRecords.QualityRecord>()
            .WithMany()
            .HasForeignKey(x => x.QualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Qms.Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SpecializedLookupDefinitionConfiguration
    : IEntityTypeConfiguration<SpecializedLookupDefinition>
{
    public void Configure(EntityTypeBuilder<SpecializedLookupDefinition> b)
    {
        b.ToTable("lookup_definition", "specialized");
        b.HasKey(x => x.Id);
        b.Property(x => x.ModuleCode).HasMaxLength(10);
        b.Property(x => x.Category).HasMaxLength(40);
        b.Property(x => x.Code).HasMaxLength(80);
        b.Property(x => x.Name).HasMaxLength(240);
        b.HasIndex(x => new
            {
                x.ModuleCode,
                x.Category,
                x.Code,
            })
            .IsUnique();
    }
}
