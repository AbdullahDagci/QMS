using System.Text;
using System.Text.Json;

namespace Qms.Domain.ElectronicForms;

public enum ElectronicFormRecordStatus
{
    Draft,
    Submitted,
    Closed,
}

public sealed class ElectronicFormRecord
{
    private ElectronicFormRecord() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid FormDefinitionId { get; private set; }
    public Guid FormVersionId { get; private set; }
    public Guid OutputTemplateId { get; private set; }
    public string FormCodeSnapshot { get; private set; } = string.Empty;
    public string FormNameSnapshot { get; private set; } = string.Empty;
    public int FormVersionNumber { get; private set; }
    public JsonDocument Data { get; private set; } = JsonDocument.Parse("{}");
    public ElectronicFormRecordStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ElectronicFormRecord Create(
        Guid qualityRecordId,
        Guid definitionId,
        Guid formVersionId,
        Guid outputTemplateId,
        string formCode,
        string formName,
        int formVersionNumber,
        JsonDocument data,
        Guid creatorId,
        string creatorName,
        DateTimeOffset now)
    {
        if (qualityRecordId == Guid.Empty || definitionId == Guid.Empty || formVersionId == Guid.Empty
            || outputTemplateId == Guid.Empty || creatorId == Guid.Empty || formVersionNumber < 1)
            throw new ArgumentException("Elektronik form kayıt kimlikleri geçersizdir.");
        Required(formCode, 64);
        Required(formName, 200);
        Required(creatorName, 200);
        ValidateData(data);
        return new ElectronicFormRecord
        {
            Id = Guid.CreateVersion7(),
            QualityRecordId = qualityRecordId,
            FormDefinitionId = definitionId,
            FormVersionId = formVersionId,
            OutputTemplateId = outputTemplateId,
            FormCodeSnapshot = formCode.Trim(),
            FormNameSnapshot = formName.Trim(),
            FormVersionNumber = formVersionNumber,
            Data = Clone(data),
            Status = ElectronicFormRecordStatus.Draft,
            CreatedByUserId = creatorId,
            CreatedByDisplayName = creatorName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void UpdateDraft(long expectedVersion, JsonDocument data, DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormRecordStatus.Draft);
        ValidateData(data);
        Data = Clone(data);
        Touch(now);
    }

    public void Submit(long expectedVersion, JsonDocument normalizedData, DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormRecordStatus.Draft);
        ValidateData(normalizedData);
        Data = Clone(normalizedData);
        Status = ElectronicFormRecordStatus.Submitted;
        SubmittedAtUtc = now;
        Touch(now);
    }

    public void Close(long expectedVersion, DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormRecordStatus.Submitted);
        Status = ElectronicFormRecordStatus.Closed;
        ClosedAtUtc = now;
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        Version++;
    }

    private void Ensure(long expectedVersion, ElectronicFormRecordStatus expectedStatus)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("Form kaydı başka bir kullanıcı tarafından değiştirildi.");
        if (Status != expectedStatus) throw new InvalidOperationException($"Form kaydı {expectedStatus} aşamasında değildir.");
    }

    private static void ValidateData(JsonDocument value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Form verisi JSON nesnesi olmalıdır.");
        if (Encoding.UTF8.GetByteCount(value.RootElement.GetRawText()) > 512 * 1024)
            throw new ArgumentException("Form verisi 512 KB sınırını aşamaz.");
    }

    private static JsonDocument Clone(JsonDocument value) => JsonDocument.Parse(value.RootElement.GetRawText());

    private static void Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"Zorunlu kayıt alanı 1-{max} karakter arasında olmalıdır.");
    }
}
