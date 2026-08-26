using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.MasterBatchRecords;
using Qms.Contracts.MasterBatchRecords;

namespace Qms.Infrastructure.MasterBatchRecords;

public sealed class MbrFinalReportService(IConfiguration configuration) : IMbrFinalReportService
{
    private const string Font = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<MbrFinalReportFile> EnsureGeneratedAsync(
        MbrDetailsResponse details,
        CancellationToken ct
    )
    {
        if (details.Record.Status is not ("Effective" or "Superseded" or "Archived"))
            throw new InvalidOperationException(
                "Nihai PDF yalnız yürürlüğe alınmış MBR sürümleri için üretilebilir."
            );
        if (details.Signatures.Count < 3)
            throw new InvalidOperationException(
                "İnceleme, onay ve yürürlük e-imzaları tamamlanmadan nihai PDF üretilemez."
            );

        var root =
            configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var dir = Path.Combine(root, "mbrs", details.Record.Id.ToString("N"));
        Directory.CreateDirectory(dir);
        var name = $"{details.Record.RecordNumber}-MBR-{details.Record.DocumentVersion}-nihai.pdf";
        var path = Path.Combine(dir, name);
        var hashPath = path + ".sha256";
        if (!File.Exists(path))
        {
            EnsureFont();
            var bytes = Build(details);
            await File.WriteAllBytesAsync(path, bytes, ct);
            await File.WriteAllTextAsync(
                hashPath,
                Convert.ToHexStringLower(SHA256.HashData(bytes)),
                Encoding.ASCII,
                ct
            );
        }
        var content = await File.ReadAllBytesAsync(path, ct);
        var actual = Convert.ToHexStringLower(SHA256.HashData(content));
        var expected = (await File.ReadAllTextAsync(hashPath, ct)).Trim();
        if (
            !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(actual),
                Encoding.ASCII.GetBytes(expected)
            )
        )
            throw new InvalidOperationException("Nihai M.12 PDF bütünlük doğrulamasını geçemedi.");
        return new(content, name, actual);
    }

    private static byte[] Build(MbrDetailsResponse d)
    {
        var r = d.Record;
        var doc = new Document
        {
            Info = { Title = $"{r.RecordNumber} Master Batch Record", Author = "QMS" },
        };
        doc.Styles["Normal"]!.Font.Name = Font;
        doc.Styles["Normal"]!.Font.Size = 8.5;
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = section.PageSetup.BottomMargin = Unit.FromCentimeter(1.3);
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(1.3);
        AddHeading(section, "QMS  |  M.12 MASTER BATCH RECORD", 10, "#0F7773");
        AddHeading(section, $"{r.ProductName} {r.Strength}", 19, "#0F5F61");
        section.AddParagraph(
            $"{r.RecordNumber} | Doküman sürümü {r.DocumentVersion} | {r.Status.ToUpperInvariant()}"
        );
        AddHeading(section, "1. Kontrollü ana bilgiler", 13, "#173C46");
        var info = Table(section, ["Alan", "Snapshot değer", "Alan", "Snapshot değer"]);
        Row(
            info,
            "Ürün",
            $"{r.ProductName} ({r.ProductCode})",
            "Dozaj formu",
            $"{r.DosageFormName} ({r.DosageFormCode})"
        );
        Row(
            info,
            "Parti büyüklüğü",
            $"{r.BatchSize} {r.BatchUnitName}",
            "Tesis / hat",
            $"{r.SiteName} / {r.LineName}"
        );
        Row(info, "Yazar", r.Author, "İnceleyen", r.Reviewer);
        Row(info, "Onaylayan", r.Approver, "Yürürlük", Format(r.EffectiveAtUtc));
        Row(
            info,
            "Revizyon gerekçesi",
            r.ChangeReason,
            "Önceki sürüm",
            r.PreviousVersionId?.ToString() ?? "İlk sürüm"
        );
        AddHeading(section, "2. Üretim talimat adımları", 13, "#173C46");
        var steps = Table(
            section,
            ["No", "Faz (snapshot)", "Talimat", "Referans", "Kritik parametre / limit"]
        );
        foreach (var x in d.Steps.OrderBy(x => x.Order))
            Row(
                steps,
                x.Order.ToString(),
                $"{x.PhaseName} ({x.PhaseCode})",
                x.Instruction,
                x.MaterialOrEquipmentReference ?? "-",
                x.IsCritical
                    ? $"{x.Parameter}: {x.LowerLimit}–{x.UpperLimit} {x.UnitName}"
                    : "Kritik değil"
            );
        AddHeading(section, "3. Elektronik imzalar", 13, "#173C46");
        var sig = Table(
            section,
            ["İmza anlamı", "İmzalayan", "Tarih / kayıt sürümü", "İçerik hash"]
        );
        foreach (var x in d.Signatures.OrderBy(x => x.SignedAtUtc))
            Row(
                sig,
                x.Meaning,
                x.Signer,
                $"{Format(x.SignedAtUtc)} / v{x.RecordVersion}",
                x.ContentHash[..Math.Min(20, x.ContentHash.Length)] + "…"
            );
        AddHeading(section, "4. Kontrollü denetim izi", 13, "#173C46");
        var trail = Table(section, ["Sürüm / olay", "İşlemi yapan", "Tarih", "Gerekçe"]);
        foreach (var x in d.AuditTrail.OrderBy(x => x.Version))
            Row(
                trail,
                $"v{x.Version} / {x.EventType}",
                x.Actor,
                Format(x.OccurredAtUtc),
                x.Reason ?? "-"
            );
        var snapshot = Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d))
        );
        var proof = section.AddParagraph();
        proof.Format.SpaceBefore = 8;
        proof.Format.Shading.Color = Color.Parse("#EAF4F2");
        proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold);
        proof.AddText(snapshot);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText($"{r.RecordNumber} · {r.DocumentVersion} | Sayfa ");
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }

    private static void AddHeading(Section s, string value, double size, string color)
    {
        var p = s.AddParagraph(value);
        p.Format.Font.Size = size;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Color.Parse(color);
        p.Format.SpaceBefore = 5;
        p.Format.SpaceAfter = 3;
    }

    private static Table Table(Section s, string[] headers)
    {
        var t = s.AddTable();
        t.Borders.Color = Color.Parse("#CBD9D7");
        t.Borders.Width = .5;
        foreach (var _ in headers)
            t.AddColumn(Unit.FromCentimeter(25.5 / headers.Length));
        var r = t.AddRow();
        r.HeadingFormat = true;
        r.Shading.Color = Color.Parse("#EAF4F2");
        for (var i = 0; i < headers.Length; i++)
            Cell(r.Cells[i], headers[i], true);
        return t;
    }

    private static void Row(Table table, params string[] values)
    {
        var r = table.AddRow();
        for (var i = 0; i < values.Length; i++)
            Cell(r.Cells[i], values[i]);
    }

    private static void Cell(Cell c, string value, bool bold = false)
    {
        var p = c.AddParagraph(value);
        p.Format.Font.Bold = bold;
        p.Format.Font.Size = 7.5;
        c.VerticalAlignment = VerticalAlignment.Center;
    }

    private static string Format(DateTimeOffset? value) =>
        value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";

    private static void EnsureFont()
    {
        if (GlobalFontSettings.FontResolver is not null)
            return;
        lock (FontLock)
            GlobalFontSettings.FontResolver ??= new Resolver();
    }

    private sealed class Resolver : IFontResolver
    {
        private static readonly string Regular = Path.Combine(
            "/usr/share/fonts/truetype/dejavu",
            "DejaVuSans.ttf"
        );
        private static readonly string Bold = Path.Combine(
            "/usr/share/fonts/truetype/dejavu",
            "DejaVuSans-Bold.ttf"
        );

        public byte[] GetFont(string faceName) =>
            File.ReadAllBytes(faceName.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular);

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "QmsBold" : "QmsRegular", false, false);
    }
}
