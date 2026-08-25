using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Documents;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class DocumentTrainingRequirementConfiguration : IEntityTypeConfiguration<DocumentTrainingRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentTrainingRequirement> b) { b.ToTable("training_requirement", "document"); b.HasKey(x => x.Id); b.Property(x => x.Position).HasMaxLength(160).IsRequired(); b.Property(x => x.AssignedUser).HasMaxLength(160).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.Evidence).HasMaxLength(1000); b.HasIndex(x => new { x.RevisionId, x.Position }).IsUnique(); }
}
