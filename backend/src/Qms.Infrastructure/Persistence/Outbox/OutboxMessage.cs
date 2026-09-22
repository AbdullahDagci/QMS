using System.Text.Json;

namespace Qms.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");

    public string Status { get; private set; } = "pending";

    public int AttemptCount { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public Guid? LockId { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public static OutboxMessage Create(Guid id, string type, JsonDocument payload,
        DateTimeOffset occurredAtUtc, DateTimeOffset? notBeforeUtc = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Outbox kimliği zorunludur.");
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);
        return new OutboxMessage
        {
            Id = id,
            Type = type.Trim(),
            Payload = payload,
            OccurredAtUtc = occurredAtUtc,
            NextAttemptAtUtc = notBeforeUtc ?? occurredAtUtc,
            Status = "pending"
        };
    }

    public void Claim(Guid lockId, DateTimeOffset now, TimeSpan lease)
    {
        if (Status == "completed" || Status == "dead-letter")
            throw new InvalidOperationException("Tamamlanmış outbox mesajı yeniden işlenemez.");
        LockId = lockId;
        LockedUntilUtc = now.Add(lease);
        Status = "processing";
        AttemptCount++;
    }

    public void Complete(Guid lockId, DateTimeOffset now)
    {
        EnsureLock(lockId);
        Status = "completed";
        ProcessedAtUtc = now;
        LockId = null;
        LockedUntilUtc = null;
        LastError = null;
    }

    public void Fail(Guid lockId, DateTimeOffset now, string error, int maximumAttempts)
    {
        EnsureLock(lockId);
        LastError = error.Length > 4000 ? error[..4000] : error;
        LockId = null;
        LockedUntilUtc = null;
        if (AttemptCount >= maximumAttempts)
        {
            Status = "dead-letter";
            return;
        }
        Status = "pending";
        NextAttemptAtUtc = now.AddSeconds(Math.Min(3600, Math.Pow(2, AttemptCount) * 15));
    }

    private void EnsureLock(Guid lockId)
    {
        if (Status != "processing" || LockId != lockId)
            throw new InvalidOperationException("Outbox mesajı bu worker tarafından kilitlenmemiştir.");
    }
}
