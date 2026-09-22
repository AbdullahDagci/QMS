using System.Text.Json;
using Qms.Infrastructure.Persistence.Outbox;

namespace Qms.IntegrationTests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Failure_UsesBackoffAndEventuallyMovesMessageToDeadLetter()
    {
        var now = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero);
        var message = OutboxMessage.Create(Guid.NewGuid(), "notification.created",
            JsonDocument.Parse("{\"userId\":\"00000000-0000-0000-0000-000000000001\"}"), now);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var lockId = Guid.NewGuid();
            message.Claim(lockId, now, TimeSpan.FromMinutes(2));
            message.Fail(lockId, now, "smtp unavailable", 3);
            Assert.Equal(attempt, message.AttemptCount);
        }

        Assert.Equal("dead-letter", message.Status);
        Assert.Equal("smtp unavailable", message.LastError);
        Assert.Throws<InvalidOperationException>(() =>
            message.Claim(Guid.NewGuid(), now, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Complete_RequiresOwningLease()
    {
        var now = DateTimeOffset.UtcNow;
        var message = OutboxMessage.Create(Guid.NewGuid(), "notification.created",
            JsonDocument.Parse("{}"), now);
        var lockId = Guid.NewGuid();
        message.Claim(lockId, now, TimeSpan.FromMinutes(2));

        Assert.Throws<InvalidOperationException>(() => message.Complete(Guid.NewGuid(), now));
        message.Complete(lockId, now);

        Assert.Equal("completed", message.Status);
        Assert.Equal(now, message.ProcessedAtUtc);
    }
}
