namespace Qms.Domain.Organization;

public sealed class RecordAccessGrant
{
    private RecordAccessGrant() { }
    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    public static RecordAccessGrant Create(Guid qualityRecordId, Guid userId, Guid assignmentId,
        DateTimeOffset grantedAtUtc)
    {
        if (qualityRecordId == Guid.Empty || userId == Guid.Empty || assignmentId == Guid.Empty)
            throw new ArgumentException("Kayıt erişim ataması kimlikleri geçersizdir.");
        return new RecordAccessGrant
        {
            Id = Guid.CreateVersion7(), QualityRecordId = qualityRecordId, UserId = userId,
            AssignmentId = assignmentId, GrantedAtUtc = grantedAtUtc
        };
    }
}
