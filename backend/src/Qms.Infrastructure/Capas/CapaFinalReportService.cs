using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.Capas;
using Qms.Contracts.Capas;

namespace Qms.Infrastructure.Capas;

public sealed class CapaFinalReportService(IConfiguration configuration) : ICapaFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<CapaFinalReportFile> EnsureGeneratedAsync(CapaDetailsResponse details, CancellationToken cancellationToken)
    {
        if (!string.Equals(details.Record.Status, "Closed", StringComparison.Ordinal))
            throw new InvalidOperationException("Nihai PDF yalnızca kapatılmış DÖF kayıtları için üretilebilir.");
        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var directory = Path.Combine(root, "capas", details.Record.Id.ToString("N")); Directory.CreateDirectory(directory);
        var safeNumber = string.Concat(details.Record.RecordNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var fileName = $"{safeNumber}-nihai-kayit-v{details.Record.Version}.pdf"; var path = Path.Combine(directory, fileName); var hashPath = path + ".sha256";
        var reportIsNew = !File.Exists(path); if (reportIsNew)
        {
            EnsureFontResolver(); var snapshotHash = SnapshotHash(details); var content = Build(details, snapshotHash);
            try { await File.WriteAllBytesAsync(path, content, cancellationToken); } catch (IOException) when (File.Exists(path)) { }
            var storedHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken)));
            try { await File.WriteAllTextAsync(hashPath, storedHash, Encoding.ASCII, cancellationToken); } catch (IOException) when (File.Exists(hashPath)) { }
        }
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken); var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var expected = (await File.ReadAllTextAsync(hashPath, cancellationToken)).Trim();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected))) throw new InvalidOperationException("Arşivlenmiş nihai PDF bütünlük doğrulamasını geçemedi.");
        await Qms.Infrastructure.Integrity.FinalReportIntegrity.SealNewOrVerifyAsync(configuration, path, actual, reportIsNew); return new(bytes, fileName, actual);
    }

    private static byte[] Build(CapaDetailsResponse details, string snapshotHash)
    {
        var r = details.Record; var document = new Document { Info = { Title = $"{r.RecordNumber} Nihai DÖF Kaydı", Author = "QMS" } };
        document.Styles["Normal"]!.Font.Name = FontFamily; document.Styles["Normal"]!.Font.Size = 9;
        var section = document.AddSection(); section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.TopMargin = Unit.FromCentimeter(1.5); section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
        var brand = section.AddParagraph("QMS  |  KALİTE YÖNETİM SİSTEMİ"); brand.Format.Font.Bold = true; brand.Format.Font.Color = Color.Parse("#0F7773");
        var title = section.AddParagraph(r.Title); title.Format.Font.Size = 20; title.Format.Font.Bold = true; title.Format.Font.Color = Color.Parse("#0F5F61");
        section.AddParagraph($"{r.RecordNumber} | M.02 DÖF Yönetimi | Sürüm {r.Version} | NİHAİ - KAPALI KAYIT");
        Heading(section, "1. Kayıt özeti"); Grid(section, ["Kaynak", r.SourceRecordNumber ?? r.SourceType, "Sorumlu", r.Owner, "Hedef", Format(r.TargetDateUtc), "Kapanış", Format(r.ClosedAtUtc)]);
        Block(section, "Problem / uygunsuzluk", r.Description); Block(section, "Kök neden", r.RootCause); Block(section, "Acil düzeltmeler", r.ImmediateActions);
        Heading(section, "2. Aksiyonlar"); var actions = Table(section, ["Tür", "Aksiyon", "Sorumlu", "Durum / kanıt"]); foreach (var a in details.Actions) Row(actions, a.ActionType, a.Description, $"{a.Owner}\n{Format(a.TargetDateUtc)}", $"{a.Status}\n{a.CompletionEvidence ?? "-"}\n{a.VerificationNote ?? "-"}");
        Heading(section, "3. Etkinlik ve kapanış"); Block(section, "Etkinlik planı", r.EffectivenessRequired ? $"{r.EffectivenessMethod} | {r.EffectivenessSample} | {r.ObservationPeriodDays} gün | {r.SuccessCriteria}\nDeğerlendiren: {r.EffectivenessEvaluator}\nSonuç: {r.EffectivenessResult ?? "-"}" : "Gerekli değil"); Block(section, "Kapanış notu", r.ClosureNote ?? "-");
        Heading(section, "4. Elektronik imzalar"); var signs = Table(section, ["Anlam", "İmzalayan", "Tarih / sürüm", "İçerik hash"]); foreach (var s in details.Signatures.OrderBy(x => x.SignedAtUtc)) Row(signs, s.Meaning, s.SignerName, $"{Format(s.SignedAtUtc)} / v{s.RecordVersion}", s.ContentHash[..Math.Min(16, s.ContentHash.Length)] + "...");
        Heading(section, "5. Denetim izi"); var audit = Table(section, ["Tarih", "Olay", "Kullanıcı", "Sürüm"]); foreach (var a in details.AuditTrail.OrderBy(x => x.OccurredAtUtc)) Row(audit, Format(a.OccurredAtUtc), a.EventType, a.Actor, a.Version.ToString());
        var proof = section.AddParagraph(); proof.Format.SpaceBefore = 12; proof.Format.Shading.Color = Color.Parse("#EAF4F2"); proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold); proof.AddText(snapshotHash);
        var footer = section.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Center; footer.AddText($"{r.RecordNumber} | Kontrollü elektronik kayıt | Sayfa "); footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }
    private static void Heading(Section s, string text) { var p = s.AddParagraph(text); p.Format.Font.Size = 13; p.Format.Font.Bold = true; p.Format.Font.Color = Color.Parse("#173C46"); p.Format.SpaceBefore = 10; p.Format.SpaceAfter = 5; }
    private static void Block(Section s, string label, string value) { var p = s.AddParagraph(); p.Format.SpaceAfter = 5; p.AddFormattedText(label + "\n", TextFormat.Bold); p.AddText(value); }
    private static void Grid(Section s, string[] values) { var t = Table(s, ["Alan", "Değer"]); for (var i = 0; i < values.Length; i += 2) Row(t, values[i], values[i + 1]); }
    private static Table Table(Section s, string[] headings) { var t = s.AddTable(); t.Borders.Color = Color.Parse("#CBD9D7"); t.Borders.Width = .5; var width = 17.4 / headings.Length; foreach (var _ in headings) t.AddColumn(Unit.FromCentimeter(width)); var row = t.AddRow(); row.Shading.Color = Color.Parse("#EAF4F2"); for (var i = 0; i < headings.Length; i++) Cell(row.Cells[i], headings[i], true); return t; }
    private static void Row(Table t, params string[] values) { var r = t.AddRow(); for (var i = 0; i < values.Length; i++) Cell(r.Cells[i], values[i]); }
    private static void Cell(Cell c, string value, bool bold = false) { var p = c.AddParagraph(value); p.Format.Font.Bold = bold; p.Format.Font.Size = 8; c.VerticalAlignment = VerticalAlignment.Center; }
    private static string SnapshotHash(CapaDetailsResponse d) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(d)));
    private static string Format(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";
    private static void EnsureFontResolver() { if (GlobalFontSettings.FontResolver is not null) return; lock (FontLock) GlobalFontSettings.FontResolver ??= new Resolver(); }
    private sealed class Resolver : IFontResolver { private static readonly string Regular = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans.ttf"); private static readonly string Bold = Path.Combine("/usr/share/fonts/truetype/dejavu", "DejaVuSans-Bold.ttf"); public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular); public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "QmsBold" : "QmsRegular", false, false); }
}
