using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.Identity;
namespace Qms.Infrastructure.Persistence.Configurations;
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder) { builder.ToTable("user_session", "identity"); builder.HasKey(x => x.Id); builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); builder.HasIndex(x => x.TokenHash).IsUnique(); builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc }); }
}
