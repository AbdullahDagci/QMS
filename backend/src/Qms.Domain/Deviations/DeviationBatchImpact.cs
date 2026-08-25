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

    public static DeviationBatchImpact Create(
        Guid deviationId,
        string batchNumber,
        bool isAffected,
        bool isLocked,
        BatchDisposition disposition,
        string rationale,
        DateTimeOffset assessedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);

        if (disposition == BatchDisposition.Pending && !isLocked)
        {
            throw new InvalidOperationException("Kararı bekleyen batch/seri kilitli olmalıdır.");
        }

        if (disposition == BatchDisposition.Release && isAffected && string.IsNullOrWhiteSpace(rationale))
        {
            throw new InvalidOperationException("Etkilenen batch/seri için serbest bırakma gerekçesi zorunludur.");
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
            AssessedAtUtc = assessedAtUtc
        };
    }
}
