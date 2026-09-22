using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.Complaints;
using Qms.Contracts.Complaints;

namespace Qms.Infrastructure.Complaints;

public sealed class ComplaintFinalReportService(IConfiguration configuration) : IComplaintFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans"; private static readonly Lock FontLock = new();
    public async Task<ComplaintFinalReportFile> EnsureGeneratedAsync(ComplaintDetailsResponse details, CancellationToken ct)
    {
        if (details.Record.Status != "Closed") throw new InvalidOperationException("Nihai PDF yalnız kapatılmış müşteri şikâyetleri için üretilebilir.");
        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files"); var dir = Path.Combine(root, "complaints", details.Record.Id.ToString("N")); Directory.CreateDirectory(dir); var safe = string.Concat(details.Record.RecordNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')); var name = $"{safe}-nihai-sikayet-v{details.Record.Version}.pdf"; var path = Path.Combine(dir, name); var hashPath = path + ".sha256";
        var reportIsNew = !File.Exists(path); if (reportIsNew) { EnsureFont(); var bytes = Build(details, SnapshotHash(details)); try { await File.WriteAllBytesAsync(path, bytes, ct); } catch (IOException) when (File.Exists(path)) { } var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, ct))); try { await File.WriteAllTextAsync(hashPath, hash, Encoding.ASCII, ct); } catch (IOException) when (File.Exists(hashPath)) { } }
        var content = await File.ReadAllBytesAsync(path, ct); var actual = Convert.ToHexStringLower(SHA256.HashData(content)); var expected = (await File.ReadAllTextAsync(hashPath, ct)).Trim(); if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected))) throw new InvalidOperationException("Nihai şikâyet PDF bütünlük doğrulamasını geçemedi."); await Qms.Infrastructure.Integrity.FinalReportIntegrity.SealNewOrVerifyAsync(configuration, path, actual, reportIsNew); return new(content, name, actual);
    }
    private static byte[] Build(ComplaintDetailsResponse d, string snapshotHash)
    {
        var r = d.Record; var doc = new Document { Info = { Title = $"{r.RecordNumber} Nihai Müşteri Şikâyeti", Author = "QMS" } }; doc.Styles["Normal"]!.Font.Name = FontFamily; doc.Styles["Normal"]!.Font.Size = 9; var s = doc.AddSection(); s.PageSetup.PageFormat = PageFormat.A4; s.PageSetup.TopMargin = Unit.FromCentimeter(1.4); s.PageSetup.BottomMargin = Unit.FromCentimeter(1.6); s.PageSetup.LeftMargin = s.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        Brand(s, "QMS  |  M.06 MÜŞTERİ ŞİKÂYETLERİ"); var title = s.AddParagraph($"{r.Product} müşteri şikâyeti"); title.Format.Font.Size = 19; title.Format.Font.Bold = true; title.Format.Font.Color = Color.Parse("#0F5F61"); s.AddParagraph($"{r.RecordNumber} | Kayıt sürümü {r.Version} | NİHAİ - KAPALI");
        Heading(s, "1. Kabul ve sınıflandırma"); Grid(s, ["Müşteri", r.CustomerName, "Ülke / kanal", $"{r.Country} / {r.Channel}", "Ürün / batch", $"{r.Product} / {r.BatchNumber ?? "-"}", "Tür / önem", $"{r.ComplaintType} / {r.Severity}", "Olay / alınma", $"{F(r.EventAtUtc)} / {F(r.ReceivedAtUtc)}", "Kayıt sahibi", r.Owner, "Trend", r.TrendFlagged ? $"Sinyal · {r.SimilarComplaintCount} benzer kayıt" : "Sinyal yok", "Hasta güvenliği", r.SuspectedAdverseEvent ? "Farmakovijilans aktarımı gerekli" : "Şüpheli advers olay yok"]);
        Block(s, "Şikâyet tanımı", r.Description); Heading(s, "2. Paralel araştırmalar"); var investigations = Table(s, ["Bölüm", "Araştırmacı", "Bulgu", "Kök neden katkısı"]); foreach (var x in d.Investigations) Row(investigations, x.Department, x.Investigator, x.Findings ?? "-", x.RootCauseContribution ?? "-");
        Heading(s, "3. Etki, kök neden ve bağlı kayıtlar"); Grid(s, ["Etki değerlendirmesi", r.ImpactAssessment ?? "-", "Doğrulanmış kök neden", r.ConfirmedRootCause ?? "-", "M.01 sapma", r.LinkedDeviationNumber ?? "Açılmadı", "M.02 DÖF", r.LinkedCapaNumber ?? (r.CapaRequired == false ? "Gerekli değil" : "Açılmadı"), "M.15 farmakovijilans", r.PharmacovigilanceRecordNumber ?? "Gerekli değil", "Kapanış notu", r.ClosureNote ?? "-"]);
        Heading(s, "4. Onaylı müşteri yanıtları"); foreach (var x in d.Responses.Where(x => x.Status == "Approved").OrderBy(x => x.ResponseType).ThenBy(x => x.VersionNumber)) Block(s, $"{x.ResponseType} v{x.VersionNumber} · {x.PreparedBy} / Onay: {x.ApprovedBy} · {F(x.ApprovedAtUtc)}", x.Content);
        Heading(s, "5. Elektronik imzalar"); var signs = Table(s, ["Anlam", "İmzalayan", "Tarih / sürüm", "İçerik hash"]); foreach (var x in d.Signatures) Row(signs, x.Meaning, x.Signer, $"{F(x.SignedAtUtc)} / v{x.RecordVersion}", x.ContentHash[..Math.Min(16, x.ContentHash.Length)] + "...");
        var proof = s.AddParagraph(); proof.Format.SpaceBefore = 10; proof.Format.Shading.Color = Color.Parse("#EAF4F2"); proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold); proof.AddText(snapshotHash); var footer = s.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Center; footer.AddText($"{r.RecordNumber} | Kontrollü elektronik kayıt | Sayfa "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField(); var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }
    private static void Brand(Section s, string value) { var p = s.AddParagraph(value); p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#0F7773"); }
    private static void Heading(Section s, string value) { var p = s.AddParagraph(value); p.Format.Font.Size = 13; p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#173C46"); p.Format.SpaceBefore = 9; p.Format.SpaceAfter = 4; }
    private static void Block(Section s, string label, string value) { var p = s.AddParagraph(); p.Format.SpaceAfter = 5; p.AddFormattedText(label + "\n", TextFormat.Bold); p.AddText(value); }
    private static void Grid(Section s, string[] values) { var t = Table(s, ["Alan", "Değer"]); for (var i = 0; i < values.Length; i += 2) Row(t, values[i], values[i + 1]); }
    private static Table Table(Section s, string[] headers) { var t = s.AddTable(); t.Borders.Color = Color.Parse("#CBD9D7"); t.Borders.Width = .5; var width = 17.4 / headers.Length; foreach (var _ in headers) t.AddColumn(Unit.FromCentimeter(width)); var row = t.AddRow(); row.Shading.Color = Color.Parse("#EAF4F2"); for (var i = 0; i < headers.Length; i++) Cell(row.Cells[i], headers[i], true); return t; }
    private static void Row(Table table, params string[] values) { var row = table.AddRow(); for (var i = 0; i < values.Length; i++) Cell(row.Cells[i], values[i]); }
    private static void Cell(Cell cell, string value, bool bold = false) { var p = cell.AddParagraph(value); p.Format.Font.Bold = bold; p.Format.Font.Size = 8; cell.VerticalAlignment = VerticalAlignment.Center; }
    private static string SnapshotHash(ComplaintDetailsResponse d) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d))); private static string F(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-"; private static void EnsureFont() { if (GlobalFontSettings.FontResolver is not null) return; lock (FontLock) GlobalFontSettings.FontResolver ??= new Resolver(); }
    private sealed class Resolver : IFontResolver { private static readonly string Regular = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans.ttf"); private static readonly string Bold = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans-Bold.ttf"); public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular); public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "QmsBold" : "QmsRegular", false, false); }
}
