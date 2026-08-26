namespace Qms.Contracts.Notifications;

public sealed record NotificationResponse(Guid Id, string ModuleCode, string Title, string Message, string? Link, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReadAtUtc);

public sealed record NotificationListResponse(int UnreadCount, IReadOnlyList<NotificationResponse> Items);
