namespace Qms.Domain.Notifications;

public sealed class UserNotification
{
    private UserNotification() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ModuleCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static UserNotification Create(Guid userId, string moduleCode, string title, string message, string? link, DateTimeOffset now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Bildirim kullanıcısı zorunludur.", nameof(userId));
        if (string.IsNullOrWhiteSpace(moduleCode)) throw new ArgumentException("Modül kodu zorunludur.", nameof(moduleCode));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Bildirim başlığı zorunludur.", nameof(title));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Bildirim mesajı zorunludur.", nameof(message));
        return new UserNotification { Id = Guid.CreateVersion7(), UserId = userId, ModuleCode = moduleCode.Trim(), Title = title.Trim(), Message = message.Trim(), Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(), CreatedAtUtc = now };
    }

    public void MarkRead(DateTimeOffset now) => ReadAtUtc ??= now;
}
