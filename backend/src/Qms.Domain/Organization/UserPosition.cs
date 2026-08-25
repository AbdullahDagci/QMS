namespace Qms.Domain.Organization;

public sealed class UserPosition
{
    private UserPosition() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }

    public static UserPosition Create(Guid userId, Guid positionId, Guid departmentId, bool isPrimary, DateTimeOffset startsAtUtc) =>
        new() { Id = Guid.CreateVersion7(), UserId = userId, PositionId = positionId, DepartmentId = departmentId, IsPrimary = isPrimary, StartsAtUtc = startsAtUtc };

    public void End(DateTimeOffset endsAtUtc) => EndsAtUtc = endsAtUtc;
}
