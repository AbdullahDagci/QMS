using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.SpecializedRecords;
using Qms.Contracts.SpecializedRecords;

namespace Qms.Infrastructure.SpecializedRecords;

public sealed class SpecializedFinalReportService(IConfiguration configuration)
    : ISpecializedFinalReportService
{
    private const string Font = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<SpecializedFinalReportFile> EnsureGeneratedAsync(
        SpecializedDetailsResponse d,
        CancellationToken ct
    )
    {
        if (d.Record.Status != "Closed" || d.Signatures.Count < 3)
            throw new InvalidOperationException(
                "Nihai PDF için kayıt kapalı ve üç e-imza tamamlanmış olmalıdır."
            );
        var root =
            configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var dir = Path.Combine(
            root,
            "specialized",
            d.Record.ModuleCode.Replace(".", ""),
            d.Record.Id.ToString("N")
        );
        Directory.CreateDirectory(dir);
        var name = $"{d.Record.RecordNumber}-nihai.pdf";
        var path = Path.Combine(dir, name);
        var hp = path + ".sha256";
        var reportIsNew = !File.Exists(path); if (reportIsNew)
        {
            EnsureFont();
            var bytes = Build(d);
            await File.WriteAllBytesAsync(path, bytes, ct);
            await File.WriteAllTextAsync(
                hp,
                Convert.ToHexStringLower(SHA256.HashData(bytes)),
                Encoding.ASCII,
                ct
            );
        }
        var content = await File.ReadAllBytesAsync(path, ct);
        var actual = Convert.ToHexStringLower(SHA256.HashData(content));
        var expected = (await File.ReadAllTextAsync(hp, ct)).Trim();
        if (
            !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(actual),
                Encoding.ASCII.GetBytes(expected)
            )
        )
            throw new InvalidOperationException("Nihai PDF bütünlük doğrulamasını geçemedi.");
        await Qms.Infrastructure.Integrity.FinalReportIntegrity.SealNewOrVerifyAsync(configuration, path, actual, reportIsNew); return new(content, name, actual);
    }

    private static byte[] Build(SpecializedDetailsResponse d)
    {
        var r = d.Record;
        var doc = new Document
        {
            Info = { Title = $"{r.RecordNumber} Nihai Kayıt", Author = "QMS" },
        };
        doc.Styles["Normal"]!.Font.Name = Font;
        doc.Styles["Normal"]!.Font.Size = 8.5;
        var s = doc.AddSection();
        s.PageSetup.PageFormat = PageFormat.A4;
        s.PageSetup.TopMargin = s.PageSetup.BottomMargin = Unit.FromCentimeter(1.4);
        s.PageSetup.LeftMargin = s.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        Heading(s, $"QMS | {r.ModuleCode} KONTROLLÜ NİHAİ KAYIT", 10, "#0F7773");
        Heading(s, r.Title, 18, "#0F5F61");
        s.AddParagraph($"{r.RecordNumber} | Kayıt sürümü {r.Version} | KAPALI");
        Heading(s, "1. Kayıt kimliği ve snapshot alanları", 13, "#173C46");
        var info = Table(s, ["Alan", "Snapshot değer"]);
        Row(info, "Tür", $"{r.TypeName} ({r.TypeCode})");
        Row(info, "Konu", $"{r.SubjectName} ({r.SubjectCode})");
        Row(info, "Kapsam", $"{r.ScopeName} ({r.ScopeCode})");
        Row(info, "Referans", r.Reference);
        Row(info, "Sorumlu / inceleyen / onaylayan", $"{r.Owner} / {r.Reviewer} / {r.Approver}");
        Row(info, "Hedef", F(r.DueAtUtc));
        Heading(s, "2. Tanım ve modül verileri", 13, "#173C46");
        s.AddParagraph(r.Description);
        var data = Table(s, ["Alan", "Değer"]);
        foreach (var x in r.StructuredData.EnumerateObject())
            Row(data, x.Name, x.Value.ToString());
        Heading(s, "3. Elektronik imzalar", 13, "#173C46");
        var sig = Table(s, ["İmza anlamı", "İmzalayan", "Tarih / sürüm", "İçerik hash"]);
        foreach (var x in d.Signatures.OrderBy(x => x.SignedAtUtc))
            Row(
                sig,
                x.Meaning,
                x.Signer,
                $"{F(x.SignedAtUtc)} / v{x.RecordVersion}",
                x.ContentHash[..Math.Min(18, x.ContentHash.Length)] + "..."
            );
        Heading(s, "4. Denetim izi", 13, "#173C46");
        var audit = Table(s, ["Sürüm / olay", "İşlemi yapan", "Tarih", "Gerekçe"]);
        foreach (var x in d.AuditTrail.OrderBy(x => x.Version))
            Row(
                audit,
                $"v{x.Version} / {x.EventType}",
                x.Actor,
                F(x.OccurredAtUtc),
                x.Reason ?? "-"
            );
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d))
        );
        var proof = s.AddParagraph();
        proof.Format.SpaceBefore = 8;
        proof.Format.Shading.Color = Color.Parse("#EAF4F2");
        proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold);
        proof.AddText(hash);
        var footer = s.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText($"{r.RecordNumber} | Sayfa ");
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }

    private static void Heading(Section s, string text, double size, string color)
    {
        var p = s.AddParagraph(text);
        p.Format.Font.Size = size;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Color.Parse(color);
        p.Format.SpaceBefore = 6;
        p.Format.SpaceAfter = 3;
    }

    private static Table Table(Section s, string[] headers)
    {
        var t = s.AddTable();
        t.Borders.Color = Color.Parse("#CBD9D7");
        t.Borders.Width = .5;
        foreach (var _ in headers)
            t.AddColumn(Unit.FromCentimeter(17.4 / headers.Length));
        var r = t.AddRow();
        r.HeadingFormat = true;
        r.Shading.Color = Color.Parse("#EAF4F2");
        for (var i = 0; i < headers.Length; i++)
            Cell(r.Cells[i], headers[i], true);
        return t;
    }

    private static void Row(Table t, params string[] values)
    {
        var r = t.AddRow();
        for (var i = 0; i < values.Length; i++)
            Cell(r.Cells[i], values[i]);
    }

    private static void Cell(Cell c, string value, bool bold = false)
    {
        var p = c.AddParagraph(value);
        p.Format.Font.Bold = bold;
        p.Format.Font.Size = 7.5;
    }

    private static string F(DateTimeOffset? v) =>
        v?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";

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

        public byte[] GetFont(string face) =>
            File.ReadAllBytes(face.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular);

        public FontResolverInfo ResolveTypeface(string family, bool bold, bool italic) =>
            new(bold ? "QmsBold" : "QmsRegular", false, false);
    }
}
