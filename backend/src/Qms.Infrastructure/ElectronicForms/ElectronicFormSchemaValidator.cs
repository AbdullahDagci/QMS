using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Qms.Infrastructure.ElectronicForms;

public sealed partial class ElectronicFormSchemaValidator
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> SupportedTypes =
    [
        "shortText", "longText", "number", "date", "dateTime", "singleSelect", "multiSelect", "checkbox"
    ];

    public FormSchema ParseAndValidateSchema(JsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        FormSchema model;
        try
        {
            model = JsonSerializer.Deserialize<FormSchema>(schema.RootElement.GetRawText(), Options)
                ?? throw new ArgumentException("Form şeması okunamadı.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Form şeması geçerli değil.", exception);
        }

        if (model.EngineVersion != 1) throw new ArgumentException("Yalnız form motoru şema sürümü 1 desteklenir.");
        if (model.Sections is null || model.Sections.Count is < 1 or > 20)
            throw new ArgumentException("Form 1 ile 20 bölüm arasında olmalıdır.");
        var sectionKeys = new HashSet<string>(StringComparer.Ordinal);
        var fields = new List<FormField>();
        foreach (var section in model.Sections)
        {
            if (section is null) throw new ArgumentException("Form bölümü boş olamaz.");
            ValidateKey(section.Key, "Bölüm");
            if (!sectionKeys.Add(section.Key)) throw new ArgumentException($"Bölüm anahtarı tekrar ediyor: {section.Key}");
            Required(section.Title, 200, "Bölüm başlığı");
            if (section.Description?.Length > 1000) throw new ArgumentException("Bölüm açıklaması 1000 karakteri aşamaz.");
            if (section.Columns is < 1 or > 2) throw new ArgumentException("Bölüm sütun sayısı 1 veya 2 olmalıdır.");
            if (section.Fields is null || section.Fields.Count is < 1 or > 50)
                throw new ArgumentException("Her bölüm 1 ile 50 alan arasında olmalıdır.");
            fields.AddRange(section.Fields);
        }

        if (fields.Count > 100) throw new ArgumentException("Bir form en fazla 100 alan içerebilir.");
        var fieldKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (field is null) throw new ArgumentException("Form alanı boş olamaz.");
            ValidateKey(field.Key, "Alan");
            if (!fieldKeys.Add(field.Key)) throw new ArgumentException($"Alan anahtarı tekrar ediyor: {field.Key}");
            Required(field.Label, 200, "Alan etiketi");
            if (!SupportedTypes.Contains(field.Type)) throw new ArgumentException($"Desteklenmeyen alan türü: {field.Type}");
            if (field.HelpText?.Length > 500 || field.Placeholder?.Length > 200)
                throw new ArgumentException($"{field.Label} alanının yardımcı metni çok uzun.");
            if (field.Width is not (6 or 12)) throw new ArgumentException($"{field.Label} alan genişliği 6 veya 12 olmalıdır.");
            if (field.MaxLength is < 1 or > 10000) throw new ArgumentException($"{field.Label} azami uzunluğu 1-10000 arasında olmalıdır.");
            if (field.Min.HasValue && field.Max.HasValue && field.Min > field.Max)
                throw new ArgumentException($"{field.Label} minimum değeri maksimumdan büyük olamaz.");
            if (field.Options is null) throw new ArgumentException($"{field.Label} alanının seçenek listesi zorunludur.");
            if (field.Type is "singleSelect" or "multiSelect") ValidateOptions(field);
        }

        foreach (var field in fields)
        {
            ValidateCondition(field.Key, field.VisibilityCondition, fieldKeys);
            ValidateCondition(field.Key, field.RequiredCondition, fieldKeys);
        }
        ValidateDocument(model.Document);
        var runCount = 0;
        foreach (var section in model.Sections) ValidateBlocks(section, model.Document, ref runCount);
        EnsureAcyclicConditions(fields);
        return model;
    }

    public JsonDocument ValidateAndNormalizeData(JsonDocument schema, JsonDocument data, bool requireRequired)
    {
        var model = ParseAndValidateSchema(schema);
        ArgumentNullException.ThrowIfNull(data);
        if (data.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Form verisi JSON nesnesi olmalıdır.");
        var fields = model.Sections.SelectMany(section => section.Fields).ToDictionary(item => item.Key, StringComparer.Ordinal);
        foreach (var property in data.RootElement.EnumerateObject())
            if (!fields.ContainsKey(property.Name)) throw new ArgumentException($"Form şemasında bulunmayan alan gönderildi: {property.Name}");

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var field in model.Sections.OrderBy(item => item.Order).SelectMany(section => section.Fields.OrderBy(item => item.Order)))
            {
                var visible = Evaluate(field.VisibilityCondition, data.RootElement);
                if (!visible) continue;
                var hasValue = data.RootElement.TryGetProperty(field.Key, out var value) && !IsEmpty(value);
                var required = field.Required || EvaluateRequired(field.RequiredCondition, data.RootElement);
                if (!hasValue)
                {
                    if (requireRequired && required) throw new ArgumentException($"{field.Label} alanı zorunludur.");
                    continue;
                }
                ValidateValue(field, value);
                writer.WritePropertyName(field.Key);
                value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        return JsonDocument.Parse(stream.ToArray());
    }

    private static void ValidateValue(FormField field, JsonElement value)
    {
        switch (field.Type)
        {
            case "shortText":
            case "longText":
                if (value.ValueKind != JsonValueKind.String) Invalid(field);
                var text = value.GetString() ?? string.Empty;
                var limit = field.MaxLength ?? (field.Type == "shortText" ? 500 : 10000);
                if (text.Length > limit) throw new ArgumentException($"{field.Label} alanı {limit} karakteri aşamaz.");
                break;
            case "date":
                if (value.ValueKind != JsonValueKind.String
                    || !DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Invalid(field);
                break;
            case "dateTime":
                if (value.ValueKind != JsonValueKind.String
                    || !DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _)) Invalid(field);
                break;
            case "number":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) Invalid(field);
                else
                {
                    if (field.Min.HasValue && number < field.Min) throw new ArgumentException($"{field.Label} en az {field.Min} olmalıdır.");
                    if (field.Max.HasValue && number > field.Max) throw new ArgumentException($"{field.Label} en fazla {field.Max} olmalıdır.");
                }
                break;
            case "checkbox":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) Invalid(field);
                break;
            case "singleSelect":
                if (value.ValueKind != JsonValueKind.String || !field.Options.Any(item => item.Value == value.GetString())) Invalid(field);
                break;
            case "multiSelect":
                if (value.ValueKind != JsonValueKind.Array) Invalid(field);
                var allowed = field.Options.Select(item => item.Value).ToHashSet(StringComparer.Ordinal);
                if (value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || !allowed.Contains(item.GetString() ?? string.Empty))) Invalid(field);
                break;
        }
    }

    private static bool Evaluate(FormCondition? condition, JsonElement data)
    {
        if (condition is null) return true;
        var exists = data.TryGetProperty(condition.FieldKey, out var actual) && !IsEmpty(actual);
        return condition.Operator switch
        {
            "hasValue" => exists,
            "equals" => exists && EqualsValue(actual, condition.Value),
            "notEquals" => !exists || !EqualsValue(actual, condition.Value),
            _ => false,
        };
    }

    private static bool EvaluateRequired(FormCondition? condition, JsonElement data) => condition is not null && Evaluate(condition, data);

    private static bool EqualsValue(JsonElement actual, JsonElement? expected)
    {
        if (!expected.HasValue) return false;
        return actual.ValueKind == expected.Value.ValueKind && actual.GetRawText() == expected.Value.GetRawText();
    }

    private static bool IsEmpty(JsonElement value) => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
        || value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())
        || value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0;

    private static void ValidateOptions(FormField field)
    {
        if (field.Options.Count is < 1 or > 100) throw new ArgumentException($"{field.Label} için 1-100 seçenek tanımlanmalıdır.");
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in field.Options)
        {
            Required(option.Value, 100, "Seçenek değeri");
            Required(option.Label, 200, "Seçenek etiketi");
            if (!values.Add(option.Value)) throw new ArgumentException($"{field.Label} alanında seçenek değeri tekrar ediyor: {option.Value}");
        }
    }

    private static void ValidateCondition(string ownKey, FormCondition? condition, HashSet<string> keys)
    {
        if (condition is null) return;
        if (!keys.Contains(condition.FieldKey) || condition.FieldKey == ownKey)
            throw new ArgumentException($"{ownKey} alanındaki koşul geçersiz bir alanı hedefliyor.");
        if (condition.Operator is not ("equals" or "notEquals" or "hasValue"))
            throw new ArgumentException($"{ownKey} alanındaki koşul operatörü desteklenmiyor.");
        if (condition.Operator != "hasValue" && !condition.Value.HasValue)
            throw new ArgumentException($"{ownKey} alanındaki koşul değeri zorunludur.");
    }

    private static void EnsureAcyclicConditions(IReadOnlyCollection<FormField> fields)
    {
        var dependencies = fields.ToDictionary(item => item.Key, item => new[] { item.VisibilityCondition?.FieldKey, item.RequiredCondition?.FieldKey }
            .Where(value => value is not null).Cast<string>().ToArray(), StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        bool Visit(string key)
        {
            if (visiting.Contains(key)) return false;
            if (!visited.Add(key)) return true;
            visiting.Add(key);
            foreach (var dependency in dependencies[key]) if (!Visit(dependency)) return false;
            visiting.Remove(key);
            return true;
        }
        foreach (var key in dependencies.Keys) if (!Visit(key)) throw new ArgumentException("Form alanı koşullarında döngü tespit edildi.");
    }

    private static void ValidateDocument(FormDocumentSettings? document)
    {
        if (document is null) return;
        if (document.PageSize != "A4") throw new ArgumentException("Yalnız A4 belge boyutu desteklenir.");
        if (document.Orientation is not ("portrait" or "landscape")) throw new ArgumentException("Belge yönü geçersizdir.");
        if (document.MarginTop is < 5 or > 50 || document.MarginRight is < 5 or > 50
            || document.MarginBottom is < 5 or > 50 || document.MarginLeft is < 5 or > 50)
            throw new ArgumentException("Belge kenar boşlukları 5-50 mm arasında olmalıdır.");
        Required(document.DefaultFontFamily, 80, "Varsayılan yazı tipi");
        if (document.DefaultFontSize is < 6 or > 72) throw new ArgumentException("Varsayılan yazı boyutu 6-72 punto arasında olmalıdır.");
    }

    private static void ValidateBlocks(FormSection section, FormDocumentSettings? document, ref int runCount)
    {
        if (section.Blocks is null or { Count: 0 }) return;
        if (section.Blocks.Count > 200) throw new ArgumentException($"{section.Title} bölümü en fazla 200 belge öğesi içerebilir.");
        var blockKeys = new HashSet<string>(StringComparer.Ordinal);
        var fieldKeys = section.Fields.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        var placedFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in section.Blocks)
        {
            if (block is null) throw new ArgumentException("Belge öğesi boş olamaz.");
            ValidateKey(block.Key, "Belge öğesi");
            if (!blockKeys.Add(block.Key)) throw new ArgumentException($"Belge öğesi anahtarı tekrar ediyor: {block.Key}");
            ValidateStyle(block.Style);
            ValidatePosition(block.Position, document);
            switch (block.Type)
            {
                case "text":
                    if ((block.Text?.Length ?? 0) > 20000) throw new ArgumentException("Metin bloğu 20000 karakteri aşamaz.");
                    ValidateRuns(block.Text, block.Runs, ref runCount);
                    break;
                case "field":
                    RegisterField(block.FieldKey, fieldKeys, placedFields, block.Key);
                    break;
                case "table":
                    ValidateTable(block, fieldKeys, placedFields, ref runCount);
                    break;
                default:
                    throw new ArgumentException($"Desteklenmeyen belge öğesi türü: {block.Type}");
            }
        }
        var missing = fieldKeys.Except(placedFields).FirstOrDefault();
        if (missing is not null) throw new ArgumentException($"{missing} alanı belge üzerinde konumlandırılmamıştır.");
    }

    private static void ValidatePosition(FormBlockPosition? position, FormDocumentSettings? document)
    {
        if (position is null) return;
        var pageWidth = document?.Orientation == "landscape" ? 297m : 210m;
        var pageHeight = document?.Orientation == "landscape" ? 210m : 297m;
        var width = pageWidth - (document?.MarginLeft ?? 18m) - (document?.MarginRight ?? 18m);
        var height = pageHeight - (document?.MarginTop ?? 18m) - (document?.MarginBottom ?? 18m) - 34m;
        if (position.X < 0 || position.Y < 0 || position.Width < 15 || position.Height < 5
            || position.X > width || position.Y > height
            || position.Width > width - position.X + .01m || position.Height > height - position.Y + .01m)
            throw new ArgumentException("Öğe konumu yazdırılabilir form alanı içinde, genişliği en az 15 mm ve yüksekliği en az 5 mm olmalıdır.");
    }

    private static void ValidateTable(FormDocumentBlock block, HashSet<string> fieldKeys, HashSet<string> placedFields, ref int runCount)
    {
        if (block.Rows is < 1 or > 20 || block.Columns is < 1 or > 10)
            throw new ArgumentException("Tablo 1-20 satır ve 1-10 sütun arasında olmalıdır.");
        if (block.HeaderRows is < 0 || block.HeaderRows > block.Rows) throw new ArgumentException("Tablo başlık satırı sayısı geçersizdir.");
        if (block.BorderWidth is < 0 or > 6) throw new ArgumentException("Tablo kenarlık kalınlığı 0-6 arasında olmalıdır.");
        ValidateColor(block.BorderColor, "Tablo kenarlık rengi");
        if (block.ColumnWidths is not null && (block.ColumnWidths.Count != block.Columns
            || block.ColumnWidths.Any(width => width is <= 0 or > 1000)))
            throw new ArgumentException("Tablo sütun genişlikleri her sütun için 0'dan büyük ve en fazla 1000 olmalıdır.");
        if (block.Cells is null || block.Cells.Count != block.Rows * block.Columns)
            throw new ArgumentException("Tablo hücre sayısı satır ve sütun sayısıyla uyumlu değildir.");
        var positions = new HashSet<(int Row, int Column)>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cell in block.Cells)
        {
            if (cell is null) throw new ArgumentException("Tablo hücresi boş olamaz.");
            ValidateKey(cell.Key, "Tablo hücresi");
            if (!keys.Add(cell.Key)) throw new ArgumentException($"Tablo hücresi anahtarı tekrar ediyor: {cell.Key}");
            if (cell.Row < 0 || cell.Row >= block.Rows || cell.Column < 0 || cell.Column >= block.Columns
                || !positions.Add((cell.Row, cell.Column))) throw new ArgumentException("Tablo hücre konumu geçersiz veya tekrar ediyor.");
            if ((cell.Text?.Length ?? 0) > 2000) throw new ArgumentException("Tablo hücresi 2000 karakteri aşamaz.");
            if (cell.RowSpan < 1 || cell.ColSpan < 1 || cell.RowSpan > block.Rows - cell.Row || cell.ColSpan > block.Columns - cell.Column)
                throw new ArgumentException("Birleştirilmiş hücre tablo sınırları içinde olmalıdır.");
            ValidateRuns(cell.Text, cell.Runs, ref runCount);
            ValidateStyle(cell.Style);
            ValidateColor(cell.BackgroundColor, "Tablo hücresi arka plan rengi");
        }

        var covered = new HashSet<(int Row, int Column)>();
        foreach (var cell in block.Cells.OrderBy(item => item.Row).ThenBy(item => item.Column))
        {
            if (covered.Contains((cell.Row, cell.Column)))
            {
                if (cell.RowSpan != 1 || cell.ColSpan != 1)
                    throw new ArgumentException("Birleştirilmiş hücreler üst üste gelemez.");
                if (!string.IsNullOrEmpty(cell.Text) || cell.FieldKey is not null || cell.Runs is { Count: > 0 })
                    throw new ArgumentException("Birleştirilmiş hücrenin kapladığı hücrelerde içerik veya form alanı bulunamaz.");
                continue;
            }
            for (var row = cell.Row; row < cell.Row + cell.RowSpan; row++)
                for (var column = cell.Column; column < cell.Column + cell.ColSpan; column++)
                {
                    if (row == cell.Row && column == cell.Column) continue;
                    if (!covered.Add((row, column))) throw new ArgumentException("Birleştirilmiş hücreler üst üste gelemez.");
                }
            if (!string.IsNullOrWhiteSpace(cell.FieldKey)) RegisterField(cell.FieldKey, fieldKeys, placedFields, cell.Key);
        }
    }

    private static void ValidateRuns(string? text, IReadOnlyList<FormTextRun>? runs, ref int runCount)
    {
        if (runs is null) return;
        runCount += runs.Count;
        if (runCount > 2000) throw new ArgumentException("Form en fazla 2000 biçimlendirilmiş metin parçası içerebilir.");
        foreach (var run in runs)
        {
            if (run is null || run.Text is null || run.Style is null)
                throw new ArgumentException("Biçimlendirilmiş metin parçası, metni ve biçimi boş olamaz.");
            ValidateStyle(run.Style);
        }
        if (!string.Equals(text ?? string.Empty, string.Concat(runs.Select(run => run.Text)), StringComparison.Ordinal))
            throw new ArgumentException("Biçimlendirilmiş metin parçaları düz metinle aynı içeriği taşımalıdır.");
    }

    private static void RegisterField(string? fieldKey, HashSet<string> available, HashSet<string> placed, string owner)
    {
        if (string.IsNullOrWhiteSpace(fieldKey) || !available.Contains(fieldKey))
            throw new ArgumentException($"{owner} geçersiz bir form alanına başvuruyor.");
        if (!placed.Add(fieldKey)) throw new ArgumentException($"{fieldKey} alanı belge üzerinde birden fazla kez kullanılmıştır.");
    }

    private static void ValidateStyle(FormTextStyle? style)
    {
        if (style is null) return;
        Required(style.FontFamily, 80, "Yazı tipi");
        if (style.FontSize is < 6 or > 72) throw new ArgumentException("Yazı boyutu 6-72 punto arasında olmalıdır.");
        if (style.Alignment is not ("left" or "center" or "right" or "justify")) throw new ArgumentException("Metin hizası geçersizdir.");
        ValidateColor(style.Color, "Yazı rengi");
        ValidateColor(style.BackgroundColor, "Metin arka plan rengi");
    }

    private static void ValidateColor(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || !ColorPattern().IsMatch(value))
            throw new ArgumentException($"{label} #RRGGBB biçiminde olmalıdır.");
    }

    private static void ValidateKey(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || !KeyPattern().IsMatch(value))
            throw new ArgumentException($"{label} anahtarı harfle başlamalı ve yalnız harf, rakam, alt çizgi içermelidir.");
    }

    private static void Required(string value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"{label} 1-{max} karakter arasında olmalıdır.");
    }

    private static void Invalid(FormField field) => throw new ArgumentException($"{field.Label} alanının değeri veya türü geçersizdir.");

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}

public sealed record FormSchema(int EngineVersion, IReadOnlyList<FormSection> Sections, FormDocumentSettings? Document = null);

public sealed record FormSection(
    string Key,
    string Title,
    string? Description,
    int Order,
    int Columns,
    IReadOnlyList<FormField> Fields,
    IReadOnlyList<FormDocumentBlock>? Blocks = null);

public sealed record FormField(
    string Key,
    string Label,
    string Type,
    bool Required,
    string? HelpText,
    string? Placeholder,
    int Width,
    int Order,
    int? MaxLength,
    decimal? Min,
    decimal? Max,
    string? Unit,
    IReadOnlyList<FormOption> Options,
    FormCondition? VisibilityCondition,
    FormCondition? RequiredCondition);

public sealed record FormOption(string Value, string Label);

public sealed record FormCondition(string FieldKey, string Operator, JsonElement? Value);

public sealed record FormDocumentSettings(
    string PageSize,
    string Orientation,
    decimal MarginTop,
    decimal MarginRight,
    decimal MarginBottom,
    decimal MarginLeft,
    string DefaultFontFamily,
    decimal DefaultFontSize);

public sealed record FormTextStyle(
    string FontFamily,
    decimal FontSize,
    bool Bold,
    bool Italic,
    bool Underline,
    string Alignment,
    string Color,
    string BackgroundColor);

public sealed record FormTextRun(string Text, FormTextStyle Style);

public sealed record FormBlockPosition(decimal X, decimal Y, decimal Width, decimal Height);

public sealed record FormDocumentBlock(
    string Key,
    string Type,
    int Order,
    string? Text = null,
    FormTextStyle? Style = null,
    string? FieldKey = null,
    int Rows = 0,
    int Columns = 0,
    int HeaderRows = 0,
    string? BorderColor = null,
    decimal BorderWidth = 0,
    IReadOnlyList<FormTableCell>? Cells = null,
    IReadOnlyList<FormTextRun>? Runs = null,
    IReadOnlyList<decimal>? ColumnWidths = null,
    FormBlockPosition? Position = null);

public sealed record FormTableCell(
    string Key,
    int Row,
    int Column,
    string? Text,
    string? FieldKey,
    FormTextStyle? Style,
    string? BackgroundColor,
    int ColSpan = 1,
    int RowSpan = 1,
    IReadOnlyList<FormTextRun>? Runs = null);
