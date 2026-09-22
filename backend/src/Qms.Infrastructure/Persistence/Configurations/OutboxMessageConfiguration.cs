using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Infrastructure.Persistence.Outbox;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_message", "integration");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(256).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.Status).HasMaxLength(32).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(4000);
        builder.HasIndex(message => new { message.Status, message.LockedUntilUtc });
        builder.HasIndex(message => new { message.Status, message.NextAttemptAtUtc });
    }
}
