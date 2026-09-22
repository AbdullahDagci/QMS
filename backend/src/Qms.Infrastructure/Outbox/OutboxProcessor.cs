using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Persistence.Outbox;

namespace Qms.Infrastructure.Outbox;

public sealed class OutboxProcessor(QmsDbContext db, TimeProvider timeProvider,
    SmtpNotificationSender sender, ILogger<OutboxProcessor> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lockId = Guid.CreateVersion7();
        List<OutboxMessage> messages;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            messages = await db.OutboxMessages.FromSqlInterpolated($"""
                SELECT * FROM integration.outbox_message
                WHERE ("Status" = 'pending' AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= {now}))
                   OR ("Status" = 'processing' AND "LockedUntilUtc" < {now})
                ORDER BY "OccurredAtUtc"
                LIMIT 25 FOR UPDATE SKIP LOCKED
                """).ToListAsync(cancellationToken);
            foreach (var message in messages) message.Claim(lockId, now, TimeSpan.FromMinutes(2));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
                message.Complete(lockId, timeProvider.GetUtcNow());
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox message {MessageId} attempt {Attempt} failed",
                    message.Id, message.AttemptCount);
                message.Fail(lockId, timeProvider.GetUtcNow(), exception.Message, 8);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        return messages.Count;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (message.Type != "notification.created")
            throw new InvalidOperationException($"Desteklenmeyen outbox mesaj türü: {message.Type}");
        var payload = message.Payload.Deserialize<NotificationPayload>(JsonOptions)
            ?? throw new InvalidOperationException("Bildirim payload'ı okunamadı.");
        var email = await db.Users.AsNoTracking().Where(user => user.Id == payload.UserId && user.IsActive)
            .Select(user => user.Email).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Bildirim alıcısının e-posta adresi bulunamadı.");
        var body = $"{payload.ModuleCode}\n\n{payload.Message}"
            + (string.IsNullOrWhiteSpace(payload.Link) ? string.Empty : $"\n\n{payload.Link}");
        await sender.SendAsync(email, payload.Title, body, cancellationToken);
    }

    private sealed record NotificationPayload(Guid UserId, string ModuleCode, string Title,
        string Message, string? Link);
}
