using System.Text;
using System.Text.Json;

namespace Qms.Domain.ElectronicForms;

public sealed class ElectronicFormOutputTemplate
{
    private ElectronicFormOutputTemplate() { }

    public Guid Id { get; private set; }
    public Guid FormVersionId { get; private set; }
    public int TemplateVersionNumber { get; private set; }
    public string TemplateType { get; private set; } = "AutoLayout";
    public JsonDocument Configuration { get; private set; } = JsonDocument.Parse("{}");
    public bool IsPublished { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ElectronicFormOutputTemplate CreateDefault(
        Guid formVersionId,
        JsonDocument configuration,
        DateTimeOffset now)
    {
        if (formVersionId == Guid.Empty) throw new ArgumentException("Form sürümü zorunludur.");
        Validate(configuration);
        return new ElectronicFormOutputTemplate
        {
            Id = Guid.CreateVersion7(),
            FormVersionId = formVersionId,
            TemplateVersionNumber = 1,
            Configuration = Clone(configuration),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void Update(long expectedVersion, JsonDocument configuration, DateTimeOffset now)
    {
        if (IsPublished) throw new InvalidOperationException("Yayımlanmış çıktı şablonu değiştirilemez.");
        if (Version != expectedVersion) throw new InvalidOperationException("Çıktı şablonu başka bir kullanıcı tarafından değiştirildi.");
        Validate(configuration);
        Configuration = Clone(configuration);
        UpdatedAtUtc = now;
        Version++;
    }

    public void Publish(DateTimeOffset now)
    {
        if (IsPublished) throw new InvalidOperationException("Çıktı şablonu zaten yayımlanmış.");
        IsPublished = true;
        UpdatedAtUtc = now;
        Version++;
    }

    private static void Validate(JsonDocument value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Çıktı şablonu yapılandırması JSON nesnesi olmalıdır.");
        if (Encoding.UTF8.GetByteCount(value.RootElement.GetRawText()) > 64 * 1024)
            throw new ArgumentException("Çıktı şablonu 64 KB sınırını aşamaz.");
    }

    private static JsonDocument Clone(JsonDocument value) => JsonDocument.Parse(value.RootElement.GetRawText());
}
