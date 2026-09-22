using System.Text;
using System.Text.Json;

namespace Qms.Domain.ElectronicForms;

public enum ElectronicFormVersionStatus
{
    Draft,
    InReview,
    Published,
}

public sealed class ElectronicFormVersion
{
    private ElectronicFormVersion() { }

    public Guid Id { get; private set; }
    public Guid FormDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public int EngineSchemaVersion { get; private set; }
    public ElectronicFormVersionStatus Status { get; private set; }
    public JsonDocument Schema { get; private set; } = JsonDocument.Parse("{}");
    public string ChangeSummary { get; private set; } = string.Empty;
    public string WorkflowType { get; private set; } = "ReviewApprove";
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public string? PublishedByDisplayName { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ElectronicFormVersion CreateDraft(
        Guid definitionId,
        int versionNumber,
        JsonDocument schema,
        string changeSummary,
        string workflowType,
        Guid creatorId,
        string creatorName,
        DateTimeOffset now)
    {
        if (definitionId == Guid.Empty || versionNumber < 1 || creatorId == Guid.Empty)
            throw new ArgumentException("Form sürümü kimliği geçersizdir.");
        Required(creatorName, 200);
        ValidateJson(schema, "Form şeması");
        Required(changeSummary, 1000);
        Required(workflowType, 64);
        return new ElectronicFormVersion
        {
            Id = Guid.CreateVersion7(),
            FormDefinitionId = definitionId,
            VersionNumber = versionNumber,
            EngineSchemaVersion = 1,
            Status = ElectronicFormVersionStatus.Draft,
            Schema = Clone(schema),
            ChangeSummary = changeSummary.Trim(),
            WorkflowType = workflowType.Trim(),
            CreatedByUserId = creatorId,
            CreatedByDisplayName = creatorName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void UpdateDraft(
        long expectedVersion,
        JsonDocument schema,
        string changeSummary,
        string workflowType,
        DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormVersionStatus.Draft);
        ValidateJson(schema, "Form şeması");
        Required(changeSummary, 1000);
        Required(workflowType, 64);
        Schema = Clone(schema);
        ChangeSummary = changeSummary.Trim();
        WorkflowType = workflowType.Trim();
        Touch(now);
    }

    public void SubmitForReview(long expectedVersion, DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormVersionStatus.Draft);
        Status = ElectronicFormVersionStatus.InReview;
        SubmittedAtUtc = now;
        Touch(now);
    }

    public void Publish(
        long expectedVersion,
        Guid publisherId,
        string publisherName,
        DateTimeOffset now)
    {
        Ensure(expectedVersion, ElectronicFormVersionStatus.InReview);
        if (publisherId == Guid.Empty || publisherId == CreatedByUserId)
            throw new InvalidOperationException("Formu hazırlayan kullanıcı aynı sürümü yayımlayamaz.");
        Required(publisherName, 200);
        Status = ElectronicFormVersionStatus.Published;
        PublishedByUserId = publisherId;
        PublishedByDisplayName = publisherName.Trim();
        PublishedAtUtc = now;
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        Version++;
    }

    private void Ensure(long expected, ElectronicFormVersionStatus status)
    {
        if (Version != expected) throw new InvalidOperationException("Form sürümü başka bir kullanıcı tarafından değiştirildi.");
        if (Status != status) throw new InvalidOperationException($"Form sürümü {status} aşamasında değildir.");
    }

    private static void ValidateJson(JsonDocument value, string label)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException($"{label} bir JSON nesnesi olmalıdır.");
        if (Encoding.UTF8.GetByteCount(value.RootElement.GetRawText()) > 256 * 1024)
            throw new ArgumentException($"{label} 256 KB sınırını aşamaz.");
    }

    private static JsonDocument Clone(JsonDocument value) => JsonDocument.Parse(value.RootElement.GetRawText());

    private static void Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"Zorunlu sürüm alanı 1-{max} karakter arasında olmalıdır.");
    }
}
