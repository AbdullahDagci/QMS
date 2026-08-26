using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.ChangeControls;
using Qms.Contracts.ChangeControls;

namespace Qms.Infrastructure.ChangeControls;

public sealed class ChangeControlFinalReportService(IConfiguration configuration) : IChangeControlFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans"; private static readonly Lock FontLock = new();
    public async Task<ChangeControlFinalReportFile> EnsureGeneratedAsync(ChangeControlDetailsResponse details, CancellationToken ct)
    {
        if (details.Record.Status != "Closed") throw new InvalidOperationException("Nihai PDF yalnızca kapatılmış değişiklik kayıtları için üretilebilir.");
        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files"); var directory = Path.Combine(root, "change-controls", details.Record.Id.ToString("N")); Directory.CreateDirectory(directory);
        var safe = string.Concat(details.Record.RecordNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')); var fileName = $"{safe}-nihai-kayit-v{details.Record.Version}.pdf"; var path = Path.Combine(directory, fileName); var hashPath = path + ".sha256";
        if (!File.Exists(path)) { EnsureFontResolver(); var bytes = Build(details, SnapshotHash(details)); try { await File.WriteAllBytesAsync(path, bytes, ct); } catch (IOException) when (File.Exists(path)) { } var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, ct))); try { await File.WriteAllTextAsync(hashPath, hash, Encoding.ASCII, ct); } catch (IOException) when (File.Exists(hashPath)) { } }
        var content = await File.ReadAllBytesAsync(path, ct); var actual = Convert.ToHexStringLower(SHA256.HashData(content)); var expected = (await File.ReadAllTextAsync(hashPath, ct)).Trim(); if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected))) throw new InvalidOperationException("Arşivlenmiş nihai PDF bütünlük doğrulamasını geçemedi."); return new(content, fileName, actual);
    }
    private static byte[] Build(ChangeControlDetailsResponse details, string snapshotHash)
    {
        var r = details.Record; var document = new Document { Info = { Title = $"{r.RecordNumber} Nihai Değişiklik Kaydı", Author = "QMS" } }; document.Styles["Normal"]!.Font.Name = FontFamily; document.Styles["Normal"]!.Font.Size = 9;
        var s = document.AddSection(); s.PageSetup.PageFormat = PageFormat.A4; s.PageSetup.TopMargin = Unit.FromCentimeter(1.5); s.PageSetup.BottomMargin = Unit.FromCentimeter(1.6); s.PageSetup.LeftMargin = s.PageSetup.RightMargin = Unit.FromCentimeter(1.55);
        var brand = s.AddParagraph("QMS  |  KALİTE YÖNETİM SİSTEMİ"); brand.Format.Font.Bold = true; brand.Format.Font.Color = Color.Parse("#0F7773"); var title = s.AddParagraph(r.Title); title.Format.Font.Size = 20; title.Format.Font.Bold = true; title.Format.Font.Color = Color.Parse("#0F5F61"); s.AddParagraph($"{r.RecordNumber} | M.03 Değişiklik Kontrolü | Sürüm {r.Version} | NİHAİ - KAPALI KAYIT");
        Heading(s, "1. Değişiklik özeti"); Grid(s, ["Tür", r.ChangeType, "Sorumlu", r.Owner, "Risk", r.RiskLevel, "Ruhsat etkisi", r.RegulatoryImpact, "Hedef", Format(r.TargetDateUtc), "Kapanış", Format(r.ClosedAtUtc)]); Block(s, "Mevcut durum", r.CurrentState); Block(s, "Onaylanan yeni durum", r.ProposedState); Block(s, "Gerekçe ve kapsam", $"{r.Justification}\n{r.Scope}"); Block(s, "Risk özeti", r.RiskSummary); Block(s, "Geri dönüş planı", r.RollbackPlan);
        Heading(s, "2. Bölüm etki değerlendirmeleri"); var assessments = Table(s, ["Bölüm", "Değerlendiren", "Karar", "Etki / gerekli aksiyon"]); foreach (var a in details.Assessments) Row(assessments, a.Department, a.Reviewer, a.Status, $"{a.ImpactSummary ?? "-"}\n{a.RequiredActions ?? "-"}");
        Heading(s, "3. Uygulama aksiyonları"); var actions = Table(s, ["Kategori", "Aksiyon", "Sorumlu", "Durum / kanıt"]); foreach (var a in details.Actions) Row(actions, a.Category, a.Description, $"{a.Owner}\n{Format(a.TargetDateUtc)}", $"{a.Status}\n{a.CompletionEvidence ?? "-"}\n{a.VerificationNote ?? "-"}");
        Heading(s, "4. Devreye alma ve kapanış"); Block(s, "Otorite onay referansı", r.AuthorityApprovalReference ?? "Gerekli değil / bulunmuyor"); Block(s, "Devreye alma", Format(r.CommissionedAtUtc)); Block(s, "Uygulama sonrası sonuç", r.PostImplementationResult ?? "-"); Block(s, "Kapanış notu", r.ClosureNote ?? "-");
        Heading(s, "5. Elektronik imzalar"); var signs = Table(s, ["Anlam", "İmzalayan", "Tarih / sürüm", "İçerik hash"]); foreach (var x in details.Signatures.OrderBy(x => x.SignedAtUtc)) Row(signs, x.Meaning, x.SignerName, $"{Format(x.SignedAtUtc)} / v{x.RecordVersion}", x.ContentHash[..Math.Min(16, x.ContentHash.Length)] + "...");
        Heading(s, "6. Denetim izi"); var audit = Table(s, ["Tarih", "Olay", "Kullanıcı", "Sürüm"]); foreach (var x in details.AuditTrail.OrderBy(x => x.OccurredAtUtc)) Row(audit, Format(x.OccurredAtUtc), x.EventType, x.Actor, x.Version.ToString());
        var proof = s.AddParagraph(); proof.Format.SpaceBefore = 12; proof.Format.Shading.Color = Color.Parse("#EAF4F2"); proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold); proof.AddText(snapshotHash); var footer = s.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Center; footer.AddText($"{r.RecordNumber} | Kontrollü elektronik kayıt | Sayfa "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField(); var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }
    private static void Heading(Section s, string value) { var p = s.AddParagraph(value); p.Format.Font.Size = 13; p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#173C46"); p.Format.SpaceBefore = 10; p.Format.SpaceAfter = 5; }
    private static void Block(Section s, string label, string value) { var p = s.AddParagraph(); p.Format.SpaceAfter = 5; p.AddFormattedText(label + "\n", TextFormat.Bold); p.AddText(value); }
    private static void Grid(Section s, string[] values) { var t = Table(s, ["Alan", "Değer"]); for (var i = 0; i < values.Length; i += 2) Row(t, values[i], values[i + 1]); }
    private static Table Table(Section s, string[] headings) { var t = s.AddTable(); t.Borders.Color = Color.Parse("#CBD9D7"); t.Borders.Width = .5; var width = 17.4 / headings.Length; foreach (var _ in headings) t.AddColumn(Unit.FromCentimeter(width)); var r = t.AddRow(); r.Shading.Color = Color.Parse("#EAF4F2"); for (var i = 0; i < headings.Length; i++) Cell(r.Cells[i], headings[i], true); return t; }
    private static void Row(Table t, params string[] values) { var r = t.AddRow(); for (var i = 0; i < values.Length; i++) Cell(r.Cells[i], values[i]); }
    private static void Cell(Cell c, string value, bool bold = false) { var p = c.AddParagraph(value); p.Format.Font.Bold = bold; p.Format.Font.Size = 8; c.VerticalAlignment = VerticalAlignment.Center; }
    private static string SnapshotHash(ChangeControlDetailsResponse d) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d))); private static string Format(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";
    private static void EnsureFontResolver() { if (GlobalFontSettings.FontResolver is not null) return; lock (FontLock) GlobalFontSettings.FontResolver ??= new Resolver(); }
    private sealed class Resolver : IFontResolver { private static readonly string Regular = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans.ttf"); private static readonly string Bold = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans-Bold.ttf"); public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular); public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "QmsBold" : "QmsRegular", false, false); }
}
