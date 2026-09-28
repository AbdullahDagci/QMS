using System.Text;
using System.Text.Json;

namespace Qms.Infrastructure.Integrity;

// PostgreSQL jsonb anahtar sırasını, boşlukları ve sayı yazımını normalize eder. Bütünlük hash'leri
// bu kanonik biçim üzerinden hesaplanır; böylece yazma anı ile veritabanından geri okuma aynı metni üretir.
internal static class CanonicalJson
{
    public static string Serialize(JsonElement element) => Encoding.UTF8.GetString(SerializeToUtf8Bytes(element));

    public static JsonDocument Canonicalize(JsonDocument document) =>
        JsonDocument.Parse(SerializeToUtf8Bytes(document.RootElement));

    private static byte[] SerializeToUtf8Bytes(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            Write(writer, element);
        return stream.ToArray();
    }

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetDecimal(out var number)) writer.WriteNumberValue(number);
                else writer.WriteRawValue(element.GetRawText());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
