using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qms.Domain.AuditTrail;

namespace Qms.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_event", "audit");
        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.AggregateType).HasMaxLength(128).IsRequired();
        builder.Property(auditEvent => auditEvent.EventType).HasMaxLength(128).IsRequired();
        builder.Property(auditEvent => auditEvent.ActorDisplayNameSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(auditEvent => auditEvent.Reason).HasMaxLength(2000);
        builder.Property(auditEvent => auditEvent.Payload).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(auditEvent => new
        {
            auditEvent.AggregateType,
            auditEvent.AggregateId,
            auditEvent.AggregateVersion
        });
        builder.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
    }
}
