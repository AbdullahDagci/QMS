namespace Qms.Domain.Deviations;

public sealed class DeviationBatchImpact
{
    private DeviationBatchImpact()
    {
    }

    public Guid Id { get; private set; }

    public Guid DeviationId { get; private set; }

    public string BatchNumber { get; private set; } = string.Empty;

    public bool IsAffected { get; private set; }

    public bool IsLocked { get; private set; }

    public BatchDisposition Disposition { get; private set; }

    public string Rationale { get; private set; } = string.Empty;

    public DateTimeOffset AssessedAtUtc { get; private set; }

    public Guid AssessedByUserId { get; private set; }

    public string AssessedByNameSnapshot { get; private set; } = string.Empty;

    public string AssessedByDepartmentSnapshot { get; private set; } = string.Empty;

    public static DeviationBatchImpact Create(
        Guid deviationId,
        string batchNumber,
        bool isAffected,
        bool isLocked,
        BatchDisposition disposition,
        string rationale,
        Guid assessedByUserId,
        string assessedByName,
        string assessedByDepartment,
        DateTimeOffset assessedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessedByName);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessedByDepartment);
        if (assessedByUserId == Guid.Empty) throw new ArgumentException("Değerlendiren kullanıcı zorunludur.", nameof(assessedByUserId));

        if (disposition is BatchDisposition.Pending or BatchDisposition.Hold && !isLocked)
        {
            throw new InvalidOperationException("Bekleyen veya bekletilen batch/seri kilitli olmalıdır.");
        }

        if (disposition == BatchDisposition.Release && isLocked)
        {
            throw new InvalidOperationException("Serbest bırakılan batch/seri kilitli kalamaz.");
        }

        if (disposition == BatchDisposition.Reject && !isAffected)
        {
            throw new InvalidOperationException("Etkilenmediği belirtilen batch/seri reddedilemez.");
        }

        return new DeviationBatchImpact
        {
            Id = Guid.CreateVersion7(),
            DeviationId = deviationId,
            BatchNumber = batchNumber.Trim(),
            IsAffected = isAffected,
            IsLocked = isLocked,
            Disposition = disposition,
            Rationale = rationale.Trim(),
            AssessedByUserId = assessedByUserId,
            AssessedByNameSnapshot = assessedByName.Trim(),
            AssessedByDepartmentSnapshot = assessedByDepartment.Trim(),
            AssessedAtUtc = assessedAtUtc
        };
    }
}
