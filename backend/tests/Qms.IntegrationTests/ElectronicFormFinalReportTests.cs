using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using Qms.Contracts.ElectronicForms;
using Qms.Infrastructure.ElectronicForms;

namespace Qms.IntegrationTests;

public sealed class ElectronicFormFinalReportTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    public async Task Designed_form_renders_merged_cells_inline_fonts_and_remains_immutable(int headerRows, bool positioned)
    {
        var root = Path.Combine(Path.GetTempPath(), "qms-form-studio-pdf-tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FileStorage:RootPath"] = root,
            ["RecordIntegrity:HmacKey"] = new string('k', 64),
        }).Build();
        using var schema = positioned ? ElectronicFormSchemaValidatorTests.PositionedDocumentSchema()
            : ElectronicFormSchemaValidatorTests.ProfessionalDocumentSchema(root =>
                root["sections"]![0]!["blocks"]![1]!["headerRows"] = headerRows);
        using var output = JsonDocument.Parse("""
            {"includeAuditTrail":false,"includeSignatures":true,"title":"Form tasarım stüdyosu"}
            """);
        using var data = JsonDocument.Parse("""{"ad":"Elif Yılmaz","aciklama":"Üretim alanı kontrol edildi."}""");
        var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
        var record = new ElectronicFormRecordListItemResponse(Guid.NewGuid(), Guid.NewGuid(), "FRM-2026-001",
            "FRM-KNT", "Kontrol formu", 1, "Closed", "Elif Yılmaz", Guid.NewGuid(), now, now, 3);
        var details = new ElectronicFormRecordDetailsResponse(record, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            schema, output, data, [], [new ElectronicFormRecordSignatureResponse(Guid.NewGuid(), "Form onayı",
                "Elif Yılmaz", now, 3, new string('a', 64), null)]);
        var service = new ElectronicFormFinalReportService(configuration);

        var first = await service.EnsureGeneratedAsync(details, CancellationToken.None);
        var second = await service.EnsureGeneratedAsync(details, CancellationToken.None);

        Assert.True(first.Content.Length > 5000);
        Assert.Equal(first.Sha256, second.Sha256);
        Assert.Equal(first.Content, second.Content);
        using var stream = new MemoryStream(first.Content);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.Equal(positioned ? 2 : 1, pdf.PageCount);
        Assert.True(pdf.Pages[0].Width > pdf.Pages[0].Height);
        if (positioned)
        {
            var translations = ContentReader.ReadContent(pdf.Pages[0]).OfType<COperator>()
                .Where(item => item.Name == "cm" && item.Operands.Count == 6).ToArray();
            foreach (var (x, y) in new[] { (40d, 60d), (93d, 95d), (30d, 158d) })
                Assert.Single(translations, item => Math.Abs(Number(item.Operands[4]) - x * 72 / 25.4) < .01
                    && Math.Abs(Number(item.Operands[5]) - y * 72 / 25.4) < .01);
        }
    }

    [Theory]
    [InlineData("field")]
    [InlineData("table")]
    [InlineData("cell")]
    [InlineData("text")]
    public async Task Oversized_positioned_content_is_printed_once_in_a_paginated_continuation(string type)
    {
        var root = Path.Combine(Path.GetTempPath(), "qms-form-studio-overflow-tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = root }).Build();
        var markers = Enumerable.Range(0, type == "table" ? 60 : 300).Select(index => $"MARKER{index:D4}").ToArray();
        var longText = string.Join("\n", markers.Select(marker => $"{marker} Kontrol tamamlandi."));
        using var schema = ElectronicFormSchemaValidatorTests.PositionedDocumentSchema(node =>
        {
            var section = node["sections"]![0]!;
            var blocks = section["blocks"]!;
            section["fields"]![1]!["maxLength"] = 10000;
            if (type == "cell")
            {
                section["fields"]![0]!["type"] = "longText";
                section["fields"]![0]!["maxLength"] = 10000;
            }
            if (type == "field")
            {
                blocks[2]!["position"]!["width"] = 40;
                blocks[2]!["position"]!["height"] = 5;
            }
            if (type == "text")
            {
                blocks[0]!["text"] = longText;
                blocks[0]!["runs"] = null;
                blocks[0]!["style"] = blocks[2]!["style"]!.DeepClone();
            }
            if (type == "table")
            {
                var table = blocks[1]!;
                var style = table["cells"]![0]!["style"]!;
                table["rows"] = 20;
                table["cells"] = new JsonArray(Enumerable.Range(0, 60).Select(index => (JsonNode)new JsonObject
                {
                    ["key"] = $"longCell{index}", ["row"] = index / 3, ["column"] = index % 3,
                    ["text"] = $"{markers[index]} " + string.Join(" ", Enumerable.Repeat("kontrol", 30)),
                    ["fieldKey"] = null, ["style"] = style.DeepClone(), ["backgroundColor"] = "#ffffff",
                }).ToArray());
                blocks.AsArray().Add(new JsonObject
                {
                    ["key"] = "adAkis", ["type"] = "field", ["fieldKey"] = "ad", ["order"] = 5,
                    ["style"] = blocks[2]!["style"]!.DeepClone(),
                });
            }
        });
        using var output = JsonDocument.Parse("""{"includeAuditTrail":true,"includeSignatures":true}""");
        using var data = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            ad = type == "cell" ? longText : "Elif Yılmaz",
            aciklama = type == "field" ? longText : "Kontrol tamamlandı.",
        }));
        var now = DateTimeOffset.UtcNow;
        var record = new ElectronicFormRecordListItemResponse(Guid.NewGuid(), Guid.NewGuid(), "FRM-OVERFLOW",
            "FRM-KNT", "Kontrol formu", 1, "Closed", "Elif Yılmaz", Guid.NewGuid(), now, now, 3);
        var details = new ElectronicFormRecordDetailsResponse(record, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            schema, output, data, [new ElectronicFormRecordAuditResponse("ElectronicFormRecordApproved", "AUDITACTOR", now, 3, "AUDITREASON")],
            [new ElectronicFormRecordSignatureResponse(Guid.NewGuid(), "Form onayı",
                "Elif Yılmaz", now, 3, new string('a', 64), null)]);

        var result = await new ElectronicFormFinalReportService(configuration).EnsureGeneratedAsync(details, CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 4);
        var pageWords = pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(PageWords).ToArray();
        Assert.Contains("devam", pageWords[0]);
        foreach (var marker in markers)
        {
            Assert.DoesNotContain(marker, pageWords[0]);
            Assert.Single(pageWords.Skip(1).SelectMany(words => words), word => word == marker);
        }
        Assert.Contains("AUDITACTOR", pageWords.SelectMany(words => words));
        Assert.Contains("AUDITREASON", pageWords.SelectMany(words => words));
        Assert.True(Directory.GetFiles(root, "*.pdf", SearchOption.AllDirectories).Length == 1);
    }

    private static string[] PageWords(PdfSharp.Pdf.PdfPage page) => ContentReader.ReadContent(page).OfType<COperator>()
        .Where(operation => operation.Name is "Tj" or "TJ")
        .SelectMany(operation => Strings(operation.Operands)).ToArray();

    private static IEnumerable<string> Strings(CSequence sequence)
    {
        foreach (var item in sequence)
        {
            if (item is CString text) yield return text.Value;
            else if (item is CSequence nested)
                foreach (var value in Strings(nested)) yield return value;
        }
    }

    private static double Number(CObject value) => value switch
    {
        CReal real => real.Value,
        CInteger integer => integer.Value,
        _ => double.NaN,
    };
}
