namespace Qms.Domain.Organization;

public sealed class Delegation
{
    private Delegation() { }

    public Guid Id { get; private set; }
    public Guid DelegatorUserId { get; private set; }
    public Guid DelegateUserId { get; private set; }
    public string Scope { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public static Delegation Create(Guid delegatorUserId, Guid delegateUserId, string scope, string reason, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, Guid createdByUserId)
    {
        if (delegatorUserId == delegateUserId) throw new ArgumentException("Kullanıcı kendisine delegasyon veremez.");
        if (endsAtUtc <= startsAtUtc) throw new ArgumentException("Delegasyon bitişi başlangıçtan sonra olmalıdır.");
        ArgumentException.ThrowIfNullOrWhiteSpace(scope); ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new Delegation { Id = Guid.CreateVersion7(), DelegatorUserId = delegatorUserId, DelegateUserId = delegateUserId, Scope = scope.Trim(), Reason = reason.Trim(), StartsAtUtc = startsAtUtc, EndsAtUtc = endsAtUtc, CreatedByUserId = createdByUserId };
    }

    public void Revoke(DateTimeOffset now) => RevokedAtUtc = now;
}
