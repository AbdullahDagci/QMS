using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.ExternalAudits;
using Qms.Contracts.ExternalAudits;

namespace Qms.Infrastructure.ExternalAudits;

public sealed class ExternalAuditFinalReportService(IConfiguration configuration) : IExternalAuditFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<ExternalAuditFinalReportFile> EnsureGeneratedAsync(ExternalAuditDetailsResponse details, CancellationToken ct)
    {
        if (details.Record.Status != "Closed") throw new InvalidOperationException("Nihai PDF yalnız kapatılmış dış denetimler için üretilebilir.");
        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var directory = Path.Combine(root, "external-audits", details.Record.Id.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeNumber = string.Concat(details.Record.RecordNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var fileName = $"{safeNumber}-nihai-dis-denetim-v{details.Record.Version}.pdf";
        var path = Path.Combine(directory, fileName);
        var hashPath = path + ".sha256";
        if (!File.Exists(path))
        {
            EnsureFont();
            var bytes = Build(details, SnapshotHash(details));
            try { await File.WriteAllBytesAsync(path, bytes, ct); } catch (IOException) when (File.Exists(path)) { }
            var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, ct)));
            try { await File.WriteAllTextAsync(hashPath, hash, Encoding.ASCII, ct); } catch (IOException) when (File.Exists(hashPath)) { }
        }
        var content = await File.ReadAllBytesAsync(path, ct);
        var actual = Convert.ToHexStringLower(SHA256.HashData(content));
        var expected = (await File.ReadAllTextAsync(hashPath, ct)).Trim();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected))) throw new InvalidOperationException("Nihai dış denetim PDF bütünlük doğrulamasını geçemedi.");
        return new(content, fileName, actual);
    }

    private static byte[] Build(ExternalAuditDetailsResponse d, string snapshotHash)
    {
        var r = d.Record;
        var document = new Document { Info = { Title = $"{r.RecordNumber} Nihai Dış Denetim", Author = "QMS" } };
        document.Styles["Normal"]!.Font.Name = FontFamily; document.Styles["Normal"]!.Font.Size = 9;
        var section = document.AddSection(); section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.TopMargin = Unit.FromCentimeter(1.4); section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6); section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        Brand(section, "QMS  |  M.08 DIŞ DENETİMLER");
        var title = section.AddParagraph(r.Title); title.Format.Font.Size = 19; title.Format.Font.Bold = true; title.Format.Font.Color = Color.Parse("#0F5F61");
        section.AddParagraph($"{r.RecordNumber} | Kayıt sürümü {r.Version} | NİHAİ - KAPALI");
        Heading(section, "1. Denetim bildirimi ve kapsam");
        Grid(section, ["Denetim türü", r.AuditKind, "Denetleyen kuruluş", r.AuditorOrganization, "Kurum / ülke", $"{(r.IsGovernmentAuthority ? "Devlet kurumu" : "Dış taraf")} / {r.AuthorityCountry}", "Resmi referans", r.OfficialReference, "Saha", r.Site, "Koordinatör", r.Owner, "Planlanan dönem", $"{F(r.PlannedStartUtc)} - {F(r.PlannedEndUtc)}", "Kapanış yetkilisi", r.AuthorizedCloser ?? "-"]);
        Block(section, "Kapsam", r.Scope);
        Heading(section, "2. Kontrollü doküman paketi");
        var docs = Table(section, ["Kod / başlık", "Gizlilik", "Durum / sürüm", "Dışa aktarım"]);
        foreach (var x in d.DocumentRequests) Row(docs, $"{x.DocumentCode}\n{x.Title}", x.Confidentiality, $"{x.Status} / v{x.ExportVersion}", F(x.ExportedAtUtc));
        Heading(section, "3. Bulgular, taahhütler ve doğrulama");
        var findings = Table(section, ["Bulgu", "Sınıf / DÖF", "Sorumlu / hedef", "Yanıt / doğrulama"]);
        foreach (var x in d.Findings) Row(findings, $"{x.Number}\n{x.Title}", $"{x.Classification} / {(x.CapaRequired ? x.LinkedCapaNumber ?? "DÖF" : "Gerekli değil")}", $"{x.Owner}\n{F(x.ResponseDueAtUtc)}", $"{x.OfficialResponse ?? "-"}\n{x.VerificationNote ?? "-"}");
        Heading(section, "4. Kapanış kanıtı");
        Grid(section, ["Kapanış mektubu", r.ClosureLetterReference ?? "-", "Alınma tarihi", F(r.ClosureLetterReceivedAtUtc), "Kabul", r.AuthorityAccepted == true ? "Kabul edildi" : "Kabul edilmedi", "Nihai not", r.ClosureNote ?? "-"]);
        Block(section, "Kanıt", r.ClosureEvidence ?? "-");
        Heading(section, "5. Elektronik imzalar");
        var signatures = Table(section, ["Anlam", "İmzalayan", "Tarih / sürüm", "İçerik hash"]);
        foreach (var x in d.Signatures) Row(signatures, x.Meaning, x.Signer, $"{F(x.SignedAtUtc)} / v{x.RecordVersion}", x.ContentHash[..Math.Min(16, x.ContentHash.Length)] + "...");
        section.AddPageBreak();
        Brand(section, $"{r.RecordNumber}  |  KONTROLLÜ DENETİM İZİ");
        Heading(section, "6. Denetim izi");
        var trail = Table(section, ["Sürüm / olay", "İşlemi yapan", "Tarih", "Gerekçe"]);
        foreach (var x in d.AuditTrail.OrderBy(x => x.Version)) Row(trail, $"v{x.Version} / {EventLabel(x.EventType)}", x.Actor, F(x.OccurredAtUtc), x.Reason ?? "-");
        var proof = section.AddParagraph(); proof.Format.SpaceBefore = 10; proof.Format.Shading.Color = Color.Parse("#EAF4F2"); proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold); proof.AddText(snapshotHash);
        var footer = section.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Center; footer.AddText($"{r.RecordNumber} | Sayfa "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }

    private static string EventLabel(string value) => value switch
    {
        "ExternalAuditCreated" => "Kayıt oluşturuldu",
        "ExternalAuditStatusChanged" => "Aşama değiştirildi",
        "ExternalAuditDocumentExported" => "Doküman kontrollü aktarıldı",
        "ExternalAuditFindingCreated" => "Bulgu açıldı",
        "ExternalAuditFindingResponseSubmitted" => "Bulgu cevabı kaydedildi",
        "ExternalAuditFindingClosed" => "Bulgu doğrulanıp kapatıldı",
        "ExternalAuditClosureLetterRecorded" => "Kapanış mektubu doğrulandı",
        _ => value.Length <= 34 ? value : value[..34]
    };
    private static void Brand(Section s, string value) { var p = s.AddParagraph(value); p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#0F7773"); }
    private static void Heading(Section s, string value) { var p = s.AddParagraph(value); p.Format.Font.Size = 13; p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#173C46"); p.Format.SpaceBefore = 9; p.Format.SpaceAfter = 4; }
    private static void Block(Section s, string label, string value) { var p = s.AddParagraph(); p.Format.SpaceAfter = 5; p.AddFormattedText(label + "\n", TextFormat.Bold); p.AddText(value); }
    private static void Grid(Section s, string[] values) { var t = Table(s, ["Alan", "Değer"]); for (var i = 0; i < values.Length; i += 2) Row(t, values[i], values[i + 1]); }
    private static Table Table(Section s, string[] headers) { var t = s.AddTable(); t.Borders.Color = Color.Parse("#CBD9D7"); t.Borders.Width = .5; var width = 17.4 / headers.Length; foreach (var _ in headers) t.AddColumn(Unit.FromCentimeter(width)); var row = t.AddRow(); row.HeadingFormat = true; row.Shading.Color = Color.Parse("#EAF4F2"); for (var i = 0; i < headers.Length; i++) Cell(row.Cells[i], headers[i], true); return t; }
    private static void Row(Table table, params string[] values) { var row = table.AddRow(); for (var i = 0; i < values.Length; i++) Cell(row.Cells[i], values[i]); }
    private static void Cell(Cell cell, string value, bool bold = false) { var p = cell.AddParagraph(value); p.Format.Font.Bold = bold; p.Format.Font.Size = 8; cell.VerticalAlignment = VerticalAlignment.Center; }
    private static string SnapshotHash(ExternalAuditDetailsResponse d) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d)));
    private static string F(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";
    private static void EnsureFont() { if (GlobalFontSettings.FontResolver is not null) return; lock (FontLock) GlobalFontSettings.FontResolver ??= new Resolver(); }
    private sealed class Resolver : IFontResolver { private static readonly string Regular = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans.ttf"); private static readonly string Bold = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans-Bold.ttf"); public byte[] GetFont(string face) => File.ReadAllBytes(face.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular); public FontResolverInfo ResolveTypeface(string family, bool bold, bool italic) => new(bold ? "QmsBold" : "QmsRegular", false, false); }
}
