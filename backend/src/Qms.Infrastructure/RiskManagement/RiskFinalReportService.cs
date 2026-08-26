using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.RiskManagement;
using Qms.Contracts.RiskManagement;

namespace Qms.Infrastructure.RiskManagement;

public sealed class RiskFinalReportService(IConfiguration configuration) : IRiskFinalReportService
{
    private const string Font = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<RiskFinalReportFile> EnsureGeneratedAsync(
        RiskDetailsResponse d,
        CancellationToken ct
    )
    {
        if (d.Record.Status != "Closed")
            throw new InvalidOperationException(
                "Nihai FMEA PDF yalnız kapatılmış kayıt için üretilebilir."
            );
        var root =
            configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var dir = Path.Combine(root, "risks", d.Record.Id.ToString("N"));
        Directory.CreateDirectory(dir);
        var name = $"{d.Record.RecordNumber}-nihai-fmea-v{d.Record.Version}.pdf";
        var path = Path.Combine(dir, name);
        var hp = path + ".sha256";
        if (!File.Exists(path))
        {
            EnsureFont();
            var bytes = Build(d, Snapshot(d));
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
            throw new InvalidOperationException("Nihai M.11 PDF bütünlük kontrolünü geçemedi.");
        return new(content, name, actual);
    }

    private static byte[] Build(RiskDetailsResponse d, string snapshot)
    {
        var r = d.Record;
        var doc = new Document
        {
            Info = { Title = $"{r.RecordNumber} Nihai FMEA", Author = "QMS" },
        };
        doc.Styles["Normal"]!.Font.Name = Font;
        doc.Styles["Normal"]!.Font.Size = 8;
        var s = doc.AddSection();
        s.PageSetup.Orientation = Orientation.Landscape;
        s.PageSetup.PageFormat = PageFormat.A4;
        s.PageSetup.TopMargin = s.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
        s.PageSetup.LeftMargin = s.PageSetup.RightMargin = Unit.FromCentimeter(1.3);
        Brand(s, "QMS  |  M.11 RİSK YÖNETİMİ · FMEA");
        var title = s.AddParagraph(r.Process);
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Color.Parse("#0F5F61");
        s.AddParagraph($"{r.RecordNumber} | Sürüm {r.Version} | NİHAİ - KAPALI");
        Heading(s, "1. Kapsam ve yönetişim");
        Grid(
            s,
            [
                "Kategori",
                r.Category,
                "Metodoloji",
                r.Methodology,
                "Matris",
                r.MatrixVersion,
                "Aksiyon eşiği",
                r.ActionThreshold.ToString(),
                "Risk sahibi",
                $"{r.Owner} · {r.OwnerDepartment}",
                "Bağımsız onaylayan",
                r.Approver,
            ]
        );
        Block(s, "Kapsam ve sınırlar", r.Scope);
        Heading(s, "2. FMEA risk tablosu");
        var table = Table(
            s,
            [
                "Hata türü / neden / etki",
                "Mevcut kontroller",
                "Başlangıç S×O×D",
                "Aksiyon / sahip / kanıt",
                "Kalıntı S×O×D / gerekçe",
            ]
        );
        foreach (var x in d.Items)
            Row(
                table,
                $"{x.FailureMode}\n{x.Cause} → {x.Effect}",
                x.ExistingControls,
                $"{x.Severity}×{x.Occurrence}×{x.Detectability} = {x.InitialRpn}",
                $"{x.Action ?? "-"}\n{x.ActionOwner ?? "-"}\n{x.ActionEvidence ?? "-"}",
                x.ResidualRpn.HasValue
                    ? $"{x.ResidualSeverity}×{x.ResidualOccurrence}×{x.ResidualDetectability} = {x.ResidualRpn}\n{x.ResidualRationale}"
                    : "-"
            );
        Heading(s, "3. Elektronik imzalar");
        var sig = Table(s, ["Anlam", "İmzalayan", "Tarih / sürüm", "İçerik hash"]);
        foreach (var x in d.Signatures)
            Row(
                sig,
                x.Meaning,
                x.Signer,
                $"{F(x.SignedAtUtc)} / v{x.RecordVersion}",
                x.ContentHash[..Math.Min(16, x.ContentHash.Length)] + "..."
            );
        s.AddPageBreak();
        Brand(s, $"{r.RecordNumber} | KONTROLLÜ DENETİM İZİ");
        var trail = Table(s, ["Sürüm / olay", "İşlemi yapan", "Tarih", "Gerekçe"]);
        foreach (var x in d.AuditTrail.OrderBy(x => x.Version))
            Row(
                trail,
                $"v{x.Version} / {x.EventType}",
                x.Actor,
                F(x.OccurredAtUtc),
                x.Reason ?? "-"
            );
        var p = s.AddParagraph();
        p.Format.SpaceBefore = 8;
        p.Format.Shading.Color = Color.Parse("#EAF4F2");
        p.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold);
        p.AddText(snapshot);
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

    private static void Brand(Section s, string v)
    {
        var p = s.AddParagraph(v);
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Color.Parse("#0F7773");
    }

    private static void Heading(Section s, string v)
    {
        var p = s.AddParagraph(v);
        p.Format.Font.Size = 12;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Color.Parse("#173C46");
        p.Format.SpaceBefore = 7;
        p.Format.SpaceAfter = 3;
    }

    private static void Block(Section s, string l, string v)
    {
        var p = s.AddParagraph();
        p.AddFormattedText(l + "\n", TextFormat.Bold);
        p.AddText(v);
    }

    private static void Grid(Section s, string[] v)
    {
        var t = Table(s, ["Alan", "Değer"]);
        for (var i = 0; i < v.Length; i += 2)
            Row(t, v[i], v[i + 1]);
    }

    private static Table Table(Section s, string[] h)
    {
        var t = s.AddTable();
        t.Borders.Color = Color.Parse("#CBD9D7");
        t.Borders.Width = .5;
        foreach (var _ in h)
            t.AddColumn(Unit.FromCentimeter(26 / h.Length));
        var r = t.AddRow();
        r.HeadingFormat = true;
        r.Shading.Color = Color.Parse("#EAF4F2");
        for (var i = 0; i < h.Length; i++)
            Cell(r.Cells[i], h[i], true);
        return t;
    }

    private static void Row(Table t, params string[] v)
    {
        var r = t.AddRow();
        for (var i = 0; i < v.Length; i++)
            Cell(r.Cells[i], v[i]);
    }

    private static void Cell(Cell c, string v, bool bold = false)
    {
        var p = c.AddParagraph(v);
        p.Format.Font.Bold = bold;
        p.Format.Font.Size = 7;
        c.VerticalAlignment = VerticalAlignment.Center;
    }

    private static string Snapshot(RiskDetailsResponse d) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d)));

    private static string F(DateTimeOffset? d) =>
        d?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";

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

        public byte[] GetFont(string f) =>
            File.ReadAllBytes(f.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular);

        public FontResolverInfo ResolveTypeface(string family, bool bold, bool italic) =>
            new(bold ? "QmsBold" : "QmsRegular", false, false);
    }
}
