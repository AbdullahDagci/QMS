using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.Deviations;
using Qms.Contracts.Deviations;

namespace Qms.Infrastructure.Deviations;

public sealed class DeviationFinalReportService(IConfiguration configuration) : IDeviationFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<DeviationFinalReportFile> EnsureGeneratedAsync(DeviationDetailsResponse details, CancellationToken cancellationToken)
    {
        if (!string.Equals(details.Record.Status, "Closed", StringComparison.Ordinal))
            throw new InvalidOperationException("Nihai PDF yalnızca kapatılmış sapma kayıtları için üretilebilir.");

        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var directory = Path.Combine(root, "deviations", details.Record.Id.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeNumber = string.Concat(details.Record.RecordNumber.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var fileName = $"{safeNumber}-nihai-kayit-v{details.Record.Version}.pdf";
        var path = Path.Combine(directory, fileName);
        var hashPath = path + ".sha256";

        var reportIsNew = !File.Exists(path); if (reportIsNew)
        {
            EnsureFontResolver();
            var snapshotHash = CanonicalSnapshotHash(details);
            var content = BuildPdf(details, snapshotHash);
            try
            {
                await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                await stream.WriteAsync(content, cancellationToken);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Aynı kapanış raporunu eşzamanlı isteyen ikinci süreç mevcut dosyayı kullanır.
            }

            if (!File.Exists(hashPath))
            {
                var storedHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken)));
                try { await File.WriteAllTextAsync(hashPath, storedHash, Encoding.ASCII, cancellationToken); }
                catch (IOException) when (File.Exists(hashPath)) { }
            }
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var actualHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var expectedHash = (await File.ReadAllTextAsync(hashPath, cancellationToken)).Trim();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actualHash), Encoding.ASCII.GetBytes(expectedHash)))
            throw new InvalidOperationException("Arşivlenmiş nihai PDF bütünlük doğrulamasını geçemedi.");

        await Qms.Infrastructure.Integrity.FinalReportIntegrity.SealNewOrVerifyAsync(configuration, path, actualHash, reportIsNew, cancellationToken);
        return new DeviationFinalReportFile(bytes, fileName, actualHash);
    }

    private static byte[] BuildPdf(DeviationDetailsResponse details, string snapshotHash)
    {
        var record = details.Record;
        var document = new Document { Info = { Title = $"{record.RecordNumber} Nihai Sapma Kaydı", Author = "QMS" } };
        document.Styles["Normal"]!.Font.Name = FontFamily;
        document.Styles["Normal"]!.Font.Size = 9;
        document.Styles["Heading1"]!.Font.Name = FontFamily;
        document.Styles["Heading1"]!.Font.Color = Color.Parse("#0F5F61");
        document.Styles["Heading2"]!.Font.Name = FontFamily;
        document.Styles["Heading2"]!.Font.Color = Color.Parse("#173C46");

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.55);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.55);

        var header = section.AddTable();
        header.Borders.Width = 0;
        header.AddColumn(Unit.FromCentimeter(12.5));
        header.AddColumn(Unit.FromCentimeter(5));
        var headerRow = header.AddRow();
        var brand = headerRow.Cells[0].AddParagraph("QMS  |  KALİTE YÖNETİM SİSTEMİ");
        brand.Format.Font.Bold = true; brand.Format.Font.Size = 10; brand.Format.Font.Color = Color.Parse("#0F7773");
        var status = headerRow.Cells[1].AddParagraph("NİHAİ - KAPALI KAYIT");
        status.Format.Alignment = ParagraphAlignment.Right; status.Format.Font.Bold = true; status.Format.Font.Color = Color.Parse("#247A55");
        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(2);
        var title = section.AddParagraph(record.Title);
        title.Style = "Heading1"; title.Format.Font.Size = 21; title.Format.SpaceAfter = Unit.FromPoint(3);
        var sub = section.AddParagraph($"{record.RecordNumber}  |  M.01 Sapma Yönetimi  |  Sürüm {record.Version}");
        sub.Format.Font.Color = Color.Parse("#52656B"); sub.Format.SpaceAfter = Unit.FromPoint(12);

        AddHeading(section, "1. Kayıt özeti");
        var summary = AddKeyValueTable(section);
        AddKeyValue(summary, "Sapma türü", record.DeviationType, "Tespit eden bölüm", record.DetectedDepartment);
        AddKeyValue(summary, "Proses aşaması", record.ProcessStage, "Risk", $"{record.Classification} - RPN {record.RiskScore}");
        AddKeyValue(summary, "Gerçekleşme", Format(record.OccurredAtUtc), "Tespit", Format(record.DetectedAtUtc));
        AddKeyValue(summary, "Kapanış", Format(record.ClosedAtUtc), "Risk matrisi", record.RiskMatrixVersion);
        AddTextBlock(section, "Sapma tanımı", record.Description);
        AddTextBlock(section, "Beklenen / onaylı durum", record.ExpectedState);
        AddTextBlock(section, "Acil aksiyon", record.ImmediateAction);

        AddHeading(section, "2. Araştırma ve kök neden");
        if (details.Investigations.Count == 0) AddMuted(section, "Araştırma kaydı bulunmuyor.");
        foreach (var item in details.Investigations)
        {
            AddTextBlock(section, $"{item.Method} - {item.RootCauseCategory}", item.RootCauseDescription);
            AddTextBlock(section, "Sonuç", $"{item.Conclusion}\n{item.InvestigatorName} / {item.InvestigatorDepartment} - {Format(item.CompletedAtUtc)}");
        }

        AddHeading(section, "3. Batch / seri etki kararları");
        if (details.BatchImpacts.Count == 0) AddMuted(section, "Batch veya seri etki değerlendirmesi bulunmuyor.");
        foreach (var item in details.BatchImpacts)
            AddTextBlock(section, $"{item.BatchNumber} - {item.Disposition}", $"Etkilendi: {YesNo(item.IsAffected)} | Kilitli: {YesNo(item.IsLocked)}\n{item.Rationale}\n{item.AssessedByName} / {item.AssessedByDepartment} - {Format(item.AssessedAtUtc)}");

        AddHeading(section, "4. Kalite kararı ve kapanış");
        AddTextBlock(section, "Ön inceleme", record.PreliminaryReviewNote ?? "Kaydedilmedi");
        AddTextBlock(section, "KG değerlendirmesi", record.QualityAssessmentNote ?? "Kaydedilmedi");
        AddTextBlock(section, "Etkinlik değerlendirmesi", record.EffectivenessRequired ? record.EffectivenessAssessmentNote ?? "Gerekli" : "Gerekli değil");
        AddTextBlock(section, "Kapanış gerekçesi", record.ClosureJustification ?? "Kaydedilmedi");
        if (details.LinkedCapas.Count > 0)
            AddTextBlock(section, "İlişkili DÖF kayıtları", string.Join("\n", details.LinkedCapas.Select(x => $"{x.RecordNumber} - {x.Title} - {x.Status}")));

        AddHeading(section, "5. Elektronik imzalar");
        var signatures = AddGrid(section, ["İmza anlamı", "İmzalayan", "Tarih / sürüm", "İçerik hash"]);
        foreach (var signature in details.Signatures.OrderBy(x => x.SignedAtUtc))
            AddGridRow(signatures, signature.Meaning, signature.SignerName, $"{Format(signature.SignedAtUtc)} / v{signature.RecordVersion}", signature.ContentHash[..Math.Min(16, signature.ContentHash.Length)] + "...");

        AddHeading(section, "6. Kronolojik denetim izi");
        var audit = AddGrid(section, ["Tarih", "İşlem", "Kullanıcı", "Sürüm"]);
        foreach (var item in details.AuditTrail.OrderBy(x => x.OccurredAtUtc))
            AddGridRow(audit, Format(item.OccurredAtUtc), AuditLabel(item.EventType), item.Actor, item.Version.ToString());

        var verification = section.AddParagraph();
        verification.Format.SpaceBefore = Unit.FromPoint(14);
        verification.Format.Shading.Color = Color.Parse("#EAF4F2");
        verification.Format.Borders.Color = Color.Parse("#9BC8C2");
        verification.Format.Borders.Width = Unit.FromPoint(.7);
        verification.Format.LeftIndent = Unit.FromPoint(7);
        verification.Format.RightIndent = Unit.FromPoint(7);
        verification.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold);
        verification.AddText(snapshotHash);

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = 7;
        footer.Format.Font.Color = Color.Parse("#64767B");
        footer.AddText($"{record.RecordNumber} | Kontrollü elektronik kayıt | Sayfa ");
        footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static void AddHeading(Section section, string text)
    {
        var paragraph = section.AddParagraph(text);
        paragraph.Style = "Heading2";
        paragraph.Format.Font.Size = 13;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.SpaceBefore = Unit.FromPoint(11);
        paragraph.Format.SpaceAfter = Unit.FromPoint(6);
        paragraph.Format.KeepWithNext = true;
    }

    private static Table AddKeyValueTable(Section section)
    {
        var table = section.AddTable(); table.Borders.Color = Color.Parse("#D6E1E0"); table.Borders.Width = .5;
        table.AddColumn(Unit.FromCentimeter(3)); table.AddColumn(Unit.FromCentimeter(5.7)); table.AddColumn(Unit.FromCentimeter(3)); table.AddColumn(Unit.FromCentimeter(5.7));
        return table;
    }

    private static void AddKeyValue(Table table, string k1, string v1, string k2, string v2)
    {
        var row = table.AddRow(); row.TopPadding = 4; row.BottomPadding = 4;
        SetCell(row.Cells[0], k1, true); SetCell(row.Cells[1], v1); SetCell(row.Cells[2], k2, true); SetCell(row.Cells[3], v2);
    }

    private static void AddTextBlock(Section section, string label, string value)
    {
        var p = section.AddParagraph(); p.Format.SpaceBefore = 5; p.Format.SpaceAfter = 3; p.Format.KeepTogether = true;
        p.AddFormattedText(label + "\n", TextFormat.Bold); p.AddText(value);
    }

    private static void AddMuted(Section section, string value) { var p = section.AddParagraph(value); p.Format.Font.Color = Color.Parse("#64767B"); }

    private static Table AddGrid(Section section, string[] headings)
    {
        var table = section.AddTable(); table.Borders.Color = Color.Parse("#CBD9D7"); table.Borders.Width = .5; table.Rows.LeftIndent = 0;
        var width = 17.4 / headings.Length;
        foreach (var _ in headings) table.AddColumn(Unit.FromCentimeter(width));
        var row = table.AddRow(); row.HeadingFormat = true; row.Shading.Color = Color.Parse("#EAF4F2"); row.TopPadding = 4; row.BottomPadding = 4;
        for (var i = 0; i < headings.Length; i++) SetCell(row.Cells[i], headings[i], true);
        return table;
    }

    private static void AddGridRow(Table table, params string[] values)
    {
        var row = table.AddRow(); row.TopPadding = 3; row.BottomPadding = 3;
        for (var i = 0; i < values.Length; i++) SetCell(row.Cells[i], values[i]);
    }

    private static void SetCell(Cell cell, string text, bool bold = false)
    {
        var p = cell.AddParagraph(text); p.Format.Font.Bold = bold; p.Format.Font.Size = 8;
        if (bold) cell.Shading.Color = Color.Parse("#F3F7F6");
    }

    private static string CanonicalSnapshotHash(DeviationDetailsResponse details) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(details)));
    private static string Format(DateTimeOffset? value) => value?.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'") ?? "-";
    private static string YesNo(bool value) => value ? "Evet" : "Hayır";
    private static string AuditLabel(string eventType) => eventType switch
    {
        "DeviationCreated" => "Taslak oluşturuldu",
        "DeviationSubmitted" => "İş akışına gönderildi",
        "DeviationInvestigationCompleted" => "Araştırma eklendi",
        "DeviationBatchImpactAssessed" => "Batch / seri etkisi değerlendirildi",
        "DeviationStatusChanged" => "Durum değiştirildi",
        _ => eventType
    };

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is not null) return;
        lock (FontLock)
        {
            GlobalFontSettings.FontResolver ??= new QmsFontResolver();
        }
    }

    private sealed class QmsFontResolver : IFontResolver
    {
        private static readonly string Regular = ResolvePath("DejaVuSans.ttf", "arial.ttf");
        private static readonly string Bold = ResolvePath("DejaVuSans-Bold.ttf", "arialbd.ttf");
        public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName.EndsWith("Bold", StringComparison.Ordinal) ? Bold : Regular);
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "QmsBold" : "QmsRegular", false, false);
        private static string ResolvePath(string linuxName, string windowsName)
        {
            var linux = Path.Combine("/usr/share/fonts/truetype/dejavu", linuxName);
            if (File.Exists(linux)) return linux;
            var windows = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), windowsName);
            if (File.Exists(windows)) return windows;
            throw new FileNotFoundException("PDF üretimi için DejaVu Sans veya Arial yazı tipi bulunamadı.");
        }
    }
}
