namespace Qms.Domain.Identity;

public sealed class UserSession
{
    private UserSession() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public static UserSession Create(Guid userId, string tokenHash, DateTimeOffset now) => new() { Id = Guid.CreateVersion7(), UserId = userId, TokenHash = tokenHash, CreatedAtUtc = now, ExpiresAtUtc = now.AddHours(12) };
    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;
}
