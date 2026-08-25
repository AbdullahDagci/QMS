using System.Text.Json;

namespace Qms.Domain.AuditTrail;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public Guid Id { get; private set; }

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public long AggregateVersion { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public Guid ActorUserId { get; private set; }

    public string ActorDisplayNameSnapshot { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Reason { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");

    public static AuditEvent Create(
        string aggregateType,
        Guid aggregateId,
        long aggregateVersion,
        string eventType,
        Guid actorUserId,
        string actorDisplayName,
        DateTimeOffset occurredAtUtc,
        string correlationId,
        JsonDocument? payload = null,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new AuditEvent
        {
            Id = Guid.CreateVersion7(),
            AggregateType = aggregateType.Trim(),
            AggregateId = aggregateId,
            AggregateVersion = aggregateVersion,
            EventType = eventType.Trim(),
            ActorUserId = actorUserId,
            ActorDisplayNameSnapshot = actorDisplayName.Trim(),
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = correlationId.Trim(),
            Payload = payload ?? JsonDocument.Parse("{}"),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        };
    }
}
