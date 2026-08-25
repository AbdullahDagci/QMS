using System.Text.Json;

namespace Qms.Domain.QualityRecords;

public sealed class QualityRecord
{
    private QualityRecord()
    {
    }

    private QualityRecord(
        Guid id,
        string recordNumber,
        string recordType,
        Guid createdByUserId,
        Guid departmentId,
        DateTimeOffset createdAtUtc,
        JsonDocument data)
    {
        Id = id;
        RecordNumber = recordNumber;
        RecordType = recordType;
        CreatedByUserId = createdByUserId;
        DepartmentId = departmentId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Data = data;
        Status = QualityRecordStatus.Draft;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string RecordNumber { get; private set; } = string.Empty;

    public string RecordType { get; private set; } = string.Empty;

    public QualityRecordStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public long Version { get; private set; }

    public JsonDocument Data { get; private set; } = JsonDocument.Parse("{}");

    public static QualityRecord Create(
        string recordNumber,
        string recordType,
        Guid createdByUserId,
        Guid departmentId,
        DateTimeOffset createdAtUtc,
        JsonDocument? data = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordType);

        return new QualityRecord(
            Guid.CreateVersion7(),
            recordNumber.Trim(),
            recordType.Trim(),
            createdByUserId,
            departmentId,
            createdAtUtc,
            data ?? JsonDocument.Parse("{}"));
    }

    public void Submit(DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(QualityRecordStatus.Draft);
        Status = QualityRecordStatus.UnderAssessment;
        Touch(occurredAtUtc);
    }

    public void Close(DateTimeOffset occurredAtUtc, bool hasOpenDependencies)
    {
        EnsureStatus(QualityRecordStatus.UnderAssessment);

        if (hasOpenDependencies)
        {
            throw new InvalidOperationException("Açık bağımlılıkları bulunan kalite kaydı kapatılamaz.");
        }

        Status = QualityRecordStatus.Closed;
        ClosedAtUtc = occurredAtUtc;
        Touch(occurredAtUtc);
    }

    private void Touch(DateTimeOffset occurredAtUtc)
    {
        UpdatedAtUtc = occurredAtUtc;
        Version++;
    }

    private void EnsureStatus(QualityRecordStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Geçersiz durum geçişi. Beklenen: {expected}, mevcut: {Status}.");
        }
    }
}
