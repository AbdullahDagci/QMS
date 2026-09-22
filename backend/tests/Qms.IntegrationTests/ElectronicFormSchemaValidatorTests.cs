using System.Text.Json;
using System.Text.Json.Nodes;
using Qms.Infrastructure.ElectronicForms;

namespace Qms.IntegrationTests;

public sealed class ElectronicFormSchemaValidatorTests
{
    private readonly ElectronicFormSchemaValidator validator = new();

    [Fact]
    public void Valid_schema_and_submission_are_normalized_in_schema_order()
    {
        using var schema = Schema();
        using var data = JsonDocument.Parse("{\"detay\":\"Kontrol edildi\",\"uygunsuzlukVar\":true}");

        using var normalized = validator.ValidateAndNormalizeData(schema, data, true);

        Assert.Equal("{\"uygunsuzlukVar\":true,\"detay\":\"Kontrol edildi\"}", normalized.RootElement.GetRawText());
    }

    [Fact]
    public void Hidden_values_are_not_persisted()
    {
        using var schema = Schema();
        using var data = JsonDocument.Parse("{\"uygunsuzlukVar\":false,\"detay\":\"istemci tarafından gizlice gönderildi\"}");

        using var normalized = validator.ValidateAndNormalizeData(schema, data, true);

        Assert.False(normalized.RootElement.TryGetProperty("detay", out _));
    }

    [Fact]
    public void Conditional_required_field_is_enforced_by_the_server()
    {
        using var schema = Schema(requiredCondition: true);
        using var data = JsonDocument.Parse("{\"uygunsuzlukVar\":true}");

        var error = Assert.Throws<ArgumentException>(() => validator.ValidateAndNormalizeData(schema, data, true));

        Assert.Contains("Detay alanı zorunludur", error.Message);
    }

    [Fact]
    public void Unknown_submission_field_is_rejected()
    {
        using var schema = Schema();
        using var data = JsonDocument.Parse("{\"uygunsuzlukVar\":false,\"yetkisizAlan\":\"x\"}");

        Assert.Throws<ArgumentException>(() => validator.ValidateAndNormalizeData(schema, data, false));
    }

    [Fact]
    public void Cyclic_conditions_are_rejected()
    {
        using var schema = JsonDocument.Parse("""
            {"engineVersion":1,"sections":[{"key":"genel","title":"Genel","description":"","order":1,"columns":1,"fields":[
              {"key":"alanA","label":"Alan A","type":"shortText","required":false,"helpText":"","placeholder":"","width":12,"order":1,"maxLength":100,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":{"fieldKey":"alanB","operator":"hasValue","value":null},"requiredCondition":null},
              {"key":"alanB","label":"Alan B","type":"shortText","required":false,"helpText":"","placeholder":"","width":12,"order":2,"maxLength":100,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":{"fieldKey":"alanA","operator":"hasValue","value":null},"requiredCondition":null}
            ]}]}
            """);

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("döngü", error.Message);
    }

    [Fact]
    public void Missing_sections_returns_validation_error_instead_of_null_reference()
    {
        using var schema = JsonDocument.Parse("{\"engineVersion\":1}");

        Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Fact]
    public void Word_style_text_table_and_field_blocks_are_accepted()
    {
        using var schema = DocumentSchema(duplicateFieldPlacement: false);

        var model = validator.ParseAndValidateSchema(schema);

        Assert.Equal("landscape", model.Document!.Orientation);
        Assert.Equal(3, model.Sections[0].Blocks!.Count);
        Assert.Equal("table", model.Sections[0].Blocks![1].Type);
    }

    [Fact]
    public void A_field_cannot_be_placed_twice_in_the_document_layout()
    {
        using var schema = DocumentSchema(duplicateFieldPlacement: true);

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("birden fazla", error.Message);
    }

    [Fact]
    public void Merged_cells_column_widths_and_inline_formatting_are_preserved()
    {
        using var schema = ProfessionalDocumentSchema();

        var model = validator.ParseAndValidateSchema(schema);

        var table = model.Sections[0].Blocks![1];
        Assert.Equal(new decimal[] { 2, 1, 1 }, table.ColumnWidths);
        Assert.Equal(2, table.Cells![0].RowSpan);
        Assert.Equal(2, table.Cells[0].ColSpan);
        Assert.True(table.Cells[0].Runs![0].Style.Bold);
        Assert.False(table.Cells[0].Runs![1].Style.Bold);
    }

    [Fact]
    public void Free_position_is_preserved_in_printable_body_coordinates()
    {
        using var schema = PositionedDocumentSchema();

        var model = validator.ParseAndValidateSchema(schema);

        Assert.Equal(new FormBlockPosition(22, 8, 95, 18), model.Sections[0].Blocks![0].Position);
        Assert.Equal(new FormBlockPosition(75, 43, 140, 40), model.Sections[0].Blocks![1].Position);
    }

    [Theory]
    [InlineData(-1, 0, 20, 10)]
    [InlineData(0, -1, 20, 10)]
    [InlineData(0, 0, 14, 10)]
    [InlineData(0, 0, 20, 4)]
    [InlineData(242, 0, 20, 10)]
    [InlineData(0, 131, 20, 10)]
    public void Free_position_must_fit_the_body_not_the_whole_page(int x, int y, int width, int height)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![0]!["position"] = new JsonObject
            { ["x"] = x, ["y"] = y, ["width"] = width, ["height"] = height });

        Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Theory]
    [InlineData("1e1000")]
    [InlineData("\"NaN\"")]
    [InlineData("\"Infinity\"")]
    public void Non_finite_position_coordinates_are_rejected(string x)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![0]!["position"] = JsonNode.Parse($$"""{"x":{{x}},"y":0,"width":20,"height":10}"""));

        Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Theory]
    [InlineData(0.000001, true)]
    [InlineData(0.01, true)]
    [InlineData(0.011, false)]
    public void Right_and_bottom_bounds_allow_only_a_hundredth_mm_rounding_tolerance(double overflow, bool accepted)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![0]!["position"] = new JsonObject
            { ["x"] = 241, ["y"] = 130, ["width"] = 20 + overflow, ["height"] = 10 + overflow });

        if (accepted) validator.ParseAndValidateSchema(schema);
        else Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Theory]
    [InlineData("text", "gizli içerik")]
    [InlineData("fieldKey", "ad")]
    public void Covered_cells_cannot_silently_hide_content(string property, string value)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![1]!["cells"]![1]![property] = value);

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("kapladığı hücrelerde içerik", error.Message);
    }

    [Fact]
    public void Overlapping_merged_cells_are_rejected()
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![1]!["cells"]![1]!["rowSpan"] = 2);

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("üst üste", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Merged_cells_must_stay_inside_the_table(int span)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![1]!["cells"]![0]!["colSpan"] = span);

        Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("[1,0,2]")]
    [InlineData("[1,-1,2]")]
    [InlineData("[1,1001,2]")]
    public void Invalid_column_widths_are_rejected(string widths)
    {
        using var schema = ProfessionalDocumentSchema(root =>
            root["sections"]![0]!["blocks"]![1]!["columnWidths"] = JsonNode.Parse(widths));

        Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rich_text_must_match_the_plain_text_fallback(bool inCell)
    {
        using var schema = ProfessionalDocumentSchema(root =>
        {
            var target = inCell ? root["sections"]![0]!["blocks"]![1]!["cells"]![0]!
                : root["sections"]![0]!["blocks"]![0]!;
            target["text"] = "Biçimlendirilmiş metinden farklı";
        });

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("aynı içeriği", error.Message);
    }

    [Fact]
    public void Rich_text_run_limit_is_enforced_across_blocks_and_cells()
    {
        using var schema = ProfessionalDocumentSchema(root =>
        {
            var heading = root["sections"]![0]!["blocks"]![0]!;
            var cell = root["sections"]![0]!["blocks"]![1]!["cells"]![0]!;
            foreach (var (target, count) in new[] { (heading, 1001), (cell, 1000) })
            {
                var style = target["style"]!;
                target["runs"] = new JsonArray(Enumerable.Range(0, count)
                    .Select(_ => (JsonNode)new JsonObject { ["text"] = "a", ["style"] = style.DeepClone() }).ToArray());
                target["text"] = new string('a', count);
            }
        });

        var error = Assert.Throws<ArgumentException>(() => validator.ParseAndValidateSchema(schema));

        Assert.Contains("2000 biçimlendirilmiş", error.Message);
    }

    internal static JsonDocument ProfessionalDocumentSchema(Action<JsonNode>? edit = null)
    {
        using var original = DocumentSchema(false);
        var root = JsonNode.Parse(original.RootElement.GetRawText())!;
        var blocks = root["sections"]![0]!["blocks"]!;
        var heading = blocks[0]!;
        heading["runs"] = new JsonArray(new JsonObject { ["text"] = "Kontrol formu", ["style"] = heading["style"]!.DeepClone() });
        var table = blocks[1]!;
        var style = table["cells"]![1]!["style"]!.DeepClone();
        var boldStyle = style.DeepClone();
        boldStyle["bold"] = true;
        boldStyle["color"] = "#b91c1c";
        table["rows"] = 3;
        table["columns"] = 3;
        table["columnWidths"] = new JsonArray(2, 1, 1);
        table["cells"] = new JsonArray(Enumerable.Range(0, 9).Select(index => (JsonNode)new JsonObject
        {
            ["key"] = $"hucre{index}", ["row"] = index / 3, ["column"] = index % 3,
            ["text"] = index == 0 ? "Kontrol açıklaması" : "", ["fieldKey"] = index == 6 ? "ad" : null,
            ["style"] = style.DeepClone(), ["backgroundColor"] = index == 0 ? "#eaf4f2" : "#ffffff",
            ["colSpan"] = index == 0 ? 2 : 1, ["rowSpan"] = index == 0 ? 2 : 1,
            ["runs"] = index == 0 ? new JsonArray(
                new JsonObject { ["text"] = "Kontrol ", ["style"] = boldStyle.DeepClone() },
                new JsonObject { ["text"] = "açıklaması", ["style"] = style.DeepClone() }) : null,
        }).ToArray());
        edit?.Invoke(root);
        return JsonDocument.Parse(root.ToJsonString());
    }

    internal static JsonDocument PositionedDocumentSchema(Action<JsonNode>? edit = null) => ProfessionalDocumentSchema(root =>
    {
        var blocks = root["sections"]![0]!["blocks"]!;
        blocks[0]!["position"] = new JsonObject { ["x"] = 22, ["y"] = 8, ["width"] = 95, ["height"] = 18 };
        blocks[1]!["position"] = new JsonObject { ["x"] = 75, ["y"] = 43, ["width"] = 140, ["height"] = 40 };
        blocks[2]!["position"] = new JsonObject { ["x"] = 12, ["y"] = 106, ["width"] = 140, ["height"] = 20 };
        blocks.AsArray().Add(new JsonObject
        {
            ["key"] = "akisMetni", ["type"] = "text", ["order"] = 4,
            ["text"] = "Akış yerleşimindeki not: tüm kontroller tamamlandı.",
            ["style"] = blocks[2]!["style"]!.DeepClone(),
        });
        edit?.Invoke(root);
    });

    private static JsonDocument Schema(bool requiredCondition = false)
    {
        var condition = requiredCondition
            ? "{\"fieldKey\":\"uygunsuzlukVar\",\"operator\":\"equals\",\"value\":true}"
            : "null";
        return JsonDocument.Parse($$$"""
            {"engineVersion":1,"sections":[{"key":"genel","title":"Genel","description":"","order":1,"columns":1,"fields":[
              {"key":"uygunsuzlukVar","label":"Uygunsuzluk var","type":"checkbox","required":true,"helpText":"","placeholder":"","width":12,"order":1,"maxLength":null,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":null,"requiredCondition":null},
              {"key":"detay","label":"Detay","type":"longText","required":false,"helpText":"","placeholder":"","width":12,"order":2,"maxLength":1000,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":{"fieldKey":"uygunsuzlukVar","operator":"equals","value":true},"requiredCondition":{{{condition}}}}
            ]}]}
            """);
    }

    private static JsonDocument DocumentSchema(bool duplicateFieldPlacement)
    {
        var duplicate = duplicateFieldPlacement
            ? "{\"key\":\"blokAlanTekrar\",\"type\":\"field\",\"order\":4,\"fieldKey\":\"aciklama\",\"style\":{\"fontFamily\":\"Arial\",\"fontSize\":10,\"bold\":true,\"italic\":false,\"underline\":false,\"alignment\":\"left\",\"color\":\"#172033\",\"backgroundColor\":\"#ffffff\"}}"
            : "";
        var separator = duplicateFieldPlacement ? "," : "";
        return JsonDocument.Parse($$$"""
            {
              "engineVersion":1,
              "document":{"pageSize":"A4","orientation":"landscape","marginTop":18,"marginRight":18,"marginBottom":18,"marginLeft":18,"defaultFontFamily":"Arial","defaultFontSize":11},
              "sections":[{"key":"genel","title":"Genel","description":"","order":1,"columns":1,
                "fields":[
                  {"key":"ad","label":"Ad","type":"shortText","required":false,"helpText":"","placeholder":"","width":12,"order":1,"maxLength":100,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":null,"requiredCondition":null},
                  {"key":"aciklama","label":"Açıklama","type":"longText","required":false,"helpText":"","placeholder":"","width":12,"order":2,"maxLength":1000,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":null,"requiredCondition":null}
                ],
                "blocks":[
                  {"key":"baslik","type":"text","order":1,"text":"Kontrol formu","style":{"fontFamily":"Georgia","fontSize":20,"bold":true,"italic":false,"underline":false,"alignment":"center","color":"#0f766e","backgroundColor":"#ffffff"}},
                  {"key":"tablo","type":"table","order":2,"rows":1,"columns":2,"headerRows":0,"borderColor":"#94a3b8","borderWidth":1,"cells":[
                    {"key":"hucreBir","row":0,"column":0,"text":"Ad","fieldKey":null,"style":{"fontFamily":"Arial","fontSize":10,"bold":true,"italic":false,"underline":false,"alignment":"left","color":"#172033","backgroundColor":"#ffffff"},"backgroundColor":"#eaf4f2"},
                    {"key":"hucreIki","row":0,"column":1,"text":"","fieldKey":"ad","style":{"fontFamily":"Arial","fontSize":10,"bold":false,"italic":false,"underline":false,"alignment":"left","color":"#172033","backgroundColor":"#ffffff"},"backgroundColor":"#ffffff"}
                  ]},
                  {"key":"blokAciklama","type":"field","order":3,"fieldKey":"aciklama","style":{"fontFamily":"Arial","fontSize":10,"bold":true,"italic":false,"underline":false,"alignment":"left","color":"#172033","backgroundColor":"#ffffff"}}
                  {{{separator}}}{{{duplicate}}}
                ]
              }]
            }
            """);
    }
}
