using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Qms.Application.ElectronicForms;
using Qms.Contracts.ElectronicForms;

namespace Qms.Infrastructure.ElectronicForms;

public sealed class ElectronicFormFinalReportService(IConfiguration configuration)
    : IElectronicFormFinalReportService
{
    private const string FontFamily = "QmsDejaVuSans";
    private static readonly Lock FontLock = new();

    public async Task<ElectronicFormFinalReportFile> EnsureGeneratedAsync(
        ElectronicFormRecordDetailsResponse details,
        CancellationToken cancellationToken)
    {
        if (details.Record.Status != "Closed")
            throw new InvalidOperationException("Nihai PDF yalnız onaylanmış elektronik form kayıtları için üretilebilir.");
        if (details.Signatures.Count == 0)
            throw new InvalidOperationException("Elektronik imza bulunmadan nihai PDF üretilemez.");
        var root = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "qms-files");
        var directory = Path.Combine(root, "electronic-forms", details.Record.Id.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeNumber = SafeFileComponent(details.Record.RecordNumber);
        var safeFormCode = SafeFileComponent(details.Record.FormCode);
        var name = $"{safeNumber}-{safeFormCode}-v{details.Record.FormVersionNumber}-nihai.pdf";
        var path = Path.Combine(directory, name);
        var hashPath = path + ".sha256";
        var reportIsNew = !File.Exists(path);
        if (reportIsNew)
        {
            EnsureFont();
            var bytes = Build(details, SnapshotHash(details));
            try { await File.WriteAllBytesAsync(path, bytes, cancellationToken); }
            catch (IOException) when (File.Exists(path)) { }
            var hash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path, cancellationToken)));
            try { await File.WriteAllTextAsync(hashPath, hash, Encoding.ASCII, cancellationToken); }
            catch (IOException) when (File.Exists(hashPath)) { }
        }
        var content = await File.ReadAllBytesAsync(path, cancellationToken);
        var actual = Convert.ToHexStringLower(SHA256.HashData(content));
        var expected = (await File.ReadAllTextAsync(hashPath, cancellationToken)).Trim();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected)))
            throw new InvalidOperationException("Nihai elektronik form PDF bütünlük doğrulamasını geçemedi.");
        await Qms.Infrastructure.Integrity.FinalReportIntegrity.SealNewOrVerifyAsync(configuration, path, actual, reportIsNew);
        return new ElectronicFormFinalReportFile(content, name, actual);
    }

    private static byte[] Build(ElectronicFormRecordDetailsResponse details, string snapshotHash)
    {
        var schema = new ElectronicFormSchemaValidator().ParseAndValidateSchema(details.Schema);
        var config = details.OutputTemplate.RootElement;
        var color = Text(config, "primaryColor", "#0F7773");
        var titleText = Text(config, "title", string.Empty);
        if (string.IsNullOrWhiteSpace(titleText)) titleText = details.Record.FormName;
        var includeEmpty = Flag(config, "includeEmptyFields", false);
        var includeAudit = Flag(config, "includeAuditTrail", true);
        var includeSignatures = Flag(config, "includeSignatures", true);
        var hasPositionedBlocks = schema.Sections.Any(item => item.Blocks?.Any(block => block.Position is not null) == true);
        var continuations = new List<FormContinuation>();
        var document = new Document { Info = { Title = $"{details.Record.RecordNumber} {titleText}", Author = "QMS" } };
        var documentSettings = schema.Document;
        document.Styles["Normal"]!.Font.Name = PdfFont(documentSettings?.DefaultFontFamily);
        document.Styles["Normal"]!.Font.Size = Unit.FromPoint((double)(documentSettings?.DefaultFontSize ?? 9));
        var section = document.AddSection();
        ConfigurePage(section, documentSettings, hasPositionedBlocks);
        if (hasPositionedBlocks)
        {
            var bodyTop = section.PageSetup.TopMargin + Unit.FromMillimeter(34);
            var header = section.Headers.Primary.AddTextFrame();
            header.RelativeHorizontal = RelativeHorizontal.Page;
            header.RelativeVertical = RelativeVertical.Page;
            header.Left = section.PageSetup.LeftMargin;
            header.Top = section.PageSetup.TopMargin;
            header.Width = PrintableWidth(section);
            header.Height = Unit.FromMillimeter(34);
            header.Margin = 0;
            AddReportHeading(header.Elements, details, titleText, color, 14);
            section.PageSetup.TopMargin = bodyTop;
            AddFooter(section, details, config);
            // Frames are anchored before any flow content, so later sections cannot move them to another page.
            foreach (var schemaSection in schema.Sections)
                foreach (var block in schemaSection.Blocks?.Where(item => item.Position is not null) ?? [])
                    RenderPositionedBlock(section, schemaSection, block, details.Data.RootElement, includeEmpty, continuations);
        }
        else
        {
            AddReportHeading(section.Elements, details, titleText, color);
            AddRecordIdentity(section, details);
        }

        foreach (var schemaSection in schema.Sections.OrderBy(item => item.Order))
        {
            Heading(section, schemaSection.Title, color);
            if (!string.IsNullOrWhiteSpace(schemaSection.Description)) section.AddParagraph(schemaSection.Description);
            if (schemaSection.Blocks is { Count: > 0 })
            {
                RenderDocumentBlocks(section, schemaSection, details.Data.RootElement, includeEmpty);
                continue;
            }
            RenderLegacyFields(section, schemaSection, details.Data.RootElement, includeEmpty);
        }

        if (continuations.Count > 0)
        {
            var continuationSection = document.AddSection();
            ConfigurePage(continuationSection, documentSettings, true);
            continuationSection.Headers.Primary = new HeaderFooter();
            Brand(continuationSection, $"{details.Record.RecordNumber} | FORM İÇERİĞİ - DEVAM", color);
            continuationSection.AddParagraph("Tasarımdaki kutuya sığmayan içerikler aşağıda eksiksiz olarak gösterilir. Form üzerindeki ek numarası ilgili içeriği belirtir.");
            foreach (var continuation in continuations)
            {
                Heading(continuationSection, $"Ek {continuation.Number}: {ContinuationLabel(continuation.Section, continuation.Block)}", color);
                RenderContinuation(continuationSection, continuation, details.Data.RootElement, includeEmpty);
            }
            AddFooter(continuationSection, details, config);
        }

        if (hasPositionedBlocks)
        {
            section = document.AddSection();
            ConfigurePage(section, documentSettings, true);
            section.Headers.Primary = new HeaderFooter();
            AddReportHeading(section.Elements, details, titleText, color);
            section.AddParagraph("Formun tasarlanan yerleşimi önceki sayfalardadır. Kayıt bilgileri, elektronik imzalar ve denetim izi bu rapor ekinde korunur.");
            AddRecordIdentity(section, details);
        }

        if (includeSignatures)
        {
            Heading(section, "Elektronik imzalar", color);
            var signatureTable = Table(section, ["İmza anlamı", "İmzalayan", "Tarih / sürüm", "İçerik hash"]);
            foreach (var signature in details.Signatures)
                Row(signatureTable, signature.Meaning, signature.Signer,
                    $"{Format(signature.SignedAtUtc)} / v{signature.RecordVersion}",
                    signature.ContentHash[..Math.Min(16, signature.ContentHash.Length)] + "...");
        }

        if (includeAudit)
        {
            section.AddPageBreak();
            Brand(section, $"{details.Record.RecordNumber}  |  KONTROLLÜ DENETİM İZİ", color);
            var audit = Table(section, ["Sürüm / olay", "İşlemi yapan", "Tarih", "Gerekçe"]);
            foreach (var item in details.AuditTrail)
                Row(audit, $"v{item.Version} / {EventLabel(item.EventType)}", item.Actor,
                    Format(item.OccurredAtUtc), item.Reason ?? "-");
        }

        var proof = section.AddParagraph();
        proof.Format.SpaceBefore = 10;
        proof.Format.Shading.Color = Color.Parse("#EAF4F2");
        proof.AddFormattedText("KAYIT SNAPSHOT SHA-256\n", TextFormat.Bold);
        proof.AddText(snapshotHash);
        AddFooter(section, details, config);
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }

    private static void ConfigurePage(Section section, FormDocumentSettings? settings, bool positionedLayout)
    {
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = settings?.Orientation == "landscape" ? Orientation.Landscape : Orientation.Portrait;
        section.PageSetup.PageWidth = Unit.FromMillimeter(settings?.Orientation == "landscape" ? 297 : 210);
        section.PageSetup.PageHeight = Unit.FromMillimeter(settings?.Orientation == "landscape" ? 210 : 297);
        section.PageSetup.TopMargin = Unit.FromMillimeter((double)(settings?.MarginTop ?? (positionedLayout ? 18 : 15)));
        section.PageSetup.BottomMargin = Unit.FromMillimeter((double)(settings?.MarginBottom ?? (positionedLayout ? 18 : 17)));
        section.PageSetup.LeftMargin = Unit.FromMillimeter((double)(settings?.MarginLeft ?? (positionedLayout ? 18 : 15.5m)));
        section.PageSetup.RightMargin = Unit.FromMillimeter((double)(settings?.MarginRight ?? (positionedLayout ? 18 : 15.5m)));
    }

    private static Unit PrintableWidth(Section section) => section.PageSetup.PageWidth - section.PageSetup.LeftMargin - section.PageSetup.RightMargin;

    private static void AddReportHeading(DocumentElements elements, ElectronicFormRecordDetailsResponse details, string titleText, string color, int titleSize = 19)
    {
        var brand = elements.AddParagraph("QMS  |  ELEKTRONİK FORM");
        brand.Format.Font.Bold = true;
        brand.Format.Font.Color = Color.Parse(color);
        var title = elements.AddParagraph(titleText);
        title.Format.Font.Size = titleSize;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Color.Parse(color);
        elements.AddParagraph($"{details.Record.RecordNumber} | {details.Record.FormCode} v{details.Record.FormVersionNumber} | NİHAİ - ONAYLI");
    }

    private static void AddRecordIdentity(Section section, ElectronicFormRecordDetailsResponse details)
    {
        var identity = Table(section, ["Alan", "Değer", "Alan", "Değer"]);
        Row(identity, "Form kodu", details.Record.FormCode, "Form sürümü", details.Record.FormVersionNumber.ToString());
        Row(identity, "Kaydı oluşturan", details.Record.CreatedBy, "Oluşturma", Format(details.Record.CreatedAtUtc));
        Row(identity, "Kayıt durumu", "Onaylı / Kapalı", "Son işlem", Format(details.Record.UpdatedAtUtc));
    }

    private static void AddFooter(Section section, ElectronicFormRecordDetailsResponse details, JsonElement config)
    {
        section.Footers.Primary = new HeaderFooter();
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddText($"{details.Record.RecordNumber} | {Text(config, "footerText", "Kontrollü elektronik kayıt")} | Sayfa ");
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
    }

    private static string Display(FormField field, JsonElement value)
    {
        if (field.Type == "checkbox") return value.GetBoolean() ? "Evet" : "Hayır";
        if (field.Type == "number") return value.GetRawText() + (string.IsNullOrWhiteSpace(field.Unit) ? string.Empty : $" {field.Unit}");
        if (field.Type == "singleSelect") return OptionLabel(field, value.GetString() ?? string.Empty);
        if (field.Type == "multiSelect") return string.Join(", ", value.EnumerateArray().Select(item => OptionLabel(field, item.GetString() ?? string.Empty)));
        if (field.Type == "date" && DateOnly.TryParse(value.GetString(), out var date)) return date.ToString("dd.MM.yyyy");
        if (field.Type == "dateTime" && DateTimeOffset.TryParse(value.GetString(), out var dateTime)) return Format(dateTime);
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
    }

    private static void RenderLegacyFields(Section section, FormSection schemaSection, JsonElement data, bool includeEmpty)
    {
        var values = Table(section, ["Alan", "Değer"]);
        var hasRows = false;
        foreach (var field in schemaSection.Fields.OrderBy(item => item.Order))
        {
            if (!TryDisplay(field, data, includeEmpty, out var value)) continue;
            Row(values, field.Label, value);
            hasRows = true;
        }
        if (!hasRows) Row(values, "Bilgi", "Bu bölümde gösterilecek değer bulunmuyor.");
    }

    private static void RenderDocumentBlocks(Section section, FormSection schemaSection, JsonElement data, bool includeEmpty)
    {
        foreach (var block in schemaSection.Blocks!.OrderBy(item => item.Order))
        {
            if (block.Position is not null)
            {
                var placeholder = section.AddParagraph();
                placeholder.Format.LineSpacingRule = LineSpacingRule.Exactly;
                placeholder.Format.LineSpacing = Unit.FromMillimeter((double)block.Position.Height);
                placeholder.Format.SpaceBefore = placeholder.Format.SpaceAfter = 0;
                continue;
            }
            RenderBlock(section.Elements, PrintableWidth(section), block, schemaSection.Fields, data, includeEmpty);
        }
    }

    private sealed record FormContinuation(int Number, FormSection Section, FormDocumentBlock Block);

    private static void RenderPositionedBlock(Section section, FormSection schemaSection, FormDocumentBlock block, JsonElement data, bool includeEmpty, List<FormContinuation> continuations)
    {
        var position = block.Position!;
        var frame = section.AddTextFrame();
        frame.RelativeHorizontal = RelativeHorizontal.Page;
        frame.RelativeVertical = RelativeVertical.Page;
        frame.Left = section.PageSetup.LeftMargin + Unit.FromMillimeter((double)position.X);
        frame.Top = section.PageSetup.TopMargin + Unit.FromMillimeter((double)position.Y);
        frame.Width = Unit.FromMillimeter((double)position.Width);
        frame.Height = Unit.FromMillimeter((double)position.Height);
        frame.Margin = 0;
        frame.WrapFormat.Style = WrapStyle.Through;
        var contentHeight = MeasureBlockHeight(section.Document!, frame.Width, schemaSection, block, data, includeEmpty);
        if (contentHeight <= frame.Height.Point + .5)
            RenderBlock(frame.Elements, frame.Width, block, schemaSection.Fields, data, includeEmpty);
        else
        {
            var continuation = new FormContinuation(continuations.Count + 1, schemaSection, block);
            continuations.Add(continuation);
            RenderContinuationNotice(frame, continuation);
        }
    }

    private static double MeasureBlockHeight(Document source, Unit width, FormSection schemaSection, FormDocumentBlock block, JsonElement data, bool includeEmpty)
    {
        var measurement = new Document();
        measurement.Styles["Normal"]!.Font = source.Styles["Normal"]!.Font.Clone();
        var section = measurement.AddSection();
        section.PageSetup.PageWidth = width;
        section.PageSetup.PageHeight = Unit.FromMillimeter(1000);
        section.PageSetup.TopMargin = section.PageSetup.BottomMargin = section.PageSetup.LeftMargin = section.PageSetup.RightMargin = 0;
        RenderBlock(section.Elements, section.PageSetup.PageWidth, block, schemaSection.Fields, data, includeEmpty);
        if (section.Elements.Count == 0) return 0;
        var renderer = new DocumentRenderer(measurement);
        renderer.PrepareDocument();
        var info = renderer.GetRenderInfoFromPage(1) ?? [];
        return renderer.FormattedDocument!.PageCount > 1 ? double.PositiveInfinity
            : info.Select(item => (double)(item.LayoutInfo.ContentArea.Y + item.LayoutInfo.ContentArea.Height)).DefaultIfEmpty(0).Max();
    }

    private static string ContinuationLabel(FormSection section, FormDocumentBlock block) => block.Type switch
    {
        "field" => section.Fields.FirstOrDefault(item => item.Key == block.FieldKey)?.Label ?? "Form alanı",
        "table" => $"{section.Title} tablosu",
        _ => $"{section.Title} metni",
    };

    private static void RenderContinuationNotice(TextFrame frame, FormContinuation continuation)
    {
        var hasLabel = frame.Height.Millimeter >= 15 && frame.Width.Millimeter >= 40;
        if (hasLabel)
        {
            var label = ContinuationLabel(continuation.Section, continuation.Block);
            var paragraph = frame.AddParagraph(label.Length > 30 ? label[..27] + "..." : label);
            paragraph.Format.Font.Size = 7;
            paragraph.Format.Font.Bold = true;
        }
        var note = frame.AddParagraph(frame.Width.Millimeter < 30 && frame.Height.Millimeter < 10
            ? $"Devam: Ek {continuation.Number}" : $"İçerik devam sayfasında. Ek {continuation.Number}");
        note.Format.Font.Size = 6;
        note.Format.Font.Color = Color.Parse("#0F7773");
        note.Format.SpaceBefore = note.Format.SpaceAfter = 0;
    }

    private static void RenderContinuation(Section target, FormContinuation continuation, JsonElement data, bool includeEmpty)
    {
        var block = continuation.Block;
        if (block.Type == "field")
        {
            var field = continuation.Section.Fields.First(item => item.Key == block.FieldKey);
            if (TryDisplay(field, data, includeEmpty, out var value)) AddContinuationValue(target, value);
            return;
        }
        if (block.Type != "table")
        {
            RenderBlock(target.Elements, PrintableWidth(target), block, continuation.Section.Fields, data, includeEmpty);
            return;
        }

        // A table row cannot split across PDF pages. Measure groups connected by vertical merges before using the table renderer.
        var availableHeight = (target.PageSetup.PageHeight - target.PageSetup.TopMargin - target.PageSetup.BottomMargin).Point;
        var groupsFit = true;
        for (var start = 0; start < block.Rows;)
        {
            var end = start + 1;
            for (var row = start; row < end; row++)
                foreach (var cell in block.Cells!.Where(cell => cell.Row == row)) end = Math.Max(end, cell.Row + cell.RowSpan);
            var group = block with
            {
                Rows = end - start, HeaderRows = 0,
                Cells = block.Cells!.Where(cell => cell.Row >= start && cell.Row < end).Select(cell => cell with { Row = cell.Row - start }).ToArray(),
            };
            if (MeasureBlockHeight(target.Document!, PrintableWidth(target), continuation.Section, group, data, includeEmpty) > availableHeight - 10)
            {
                groupsFit = false;
                break;
            }
            start = end;
        }
        if (groupsFit)
        {
            RenderDesignedTable(target.Elements, PrintableWidth(target), block with { HeaderRows = 0 }, continuation.Section.Fields, data, includeEmpty);
            return;
        }

        target.AddParagraph("Tablonun uzun hücreleri, kesilmeden okunabilmeleri için satır ve sütun sırasıyla gösterilir.");
        var covered = new HashSet<(int Row, int Column)>();
        foreach (var cell in block.Cells!.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
        {
            if (covered.Contains((cell.Row, cell.Column))) continue;
            for (var row = cell.Row; row < cell.Row + cell.RowSpan; row++)
                for (var column = cell.Column; column < cell.Column + cell.ColSpan; column++) covered.Add((row, column));
            var heading = target.AddParagraph($"Satır {cell.Row + 1}, sütun {cell.Column + 1}");
            heading.Format.Font.Bold = true;
            heading.Format.KeepWithNext = true;
            if (!string.IsNullOrWhiteSpace(cell.FieldKey))
            {
                var field = continuation.Section.Fields.First(item => item.Key == cell.FieldKey);
                heading.AddText($" - {field.Label}");
                if (TryDisplay(field, data, includeEmpty, out var value)) AddContinuationValue(target, value);
            }
            else
            {
                var paragraph = target.AddParagraph();
                ApplyStyle(paragraph, cell.Style);
                AddText(paragraph, cell.Text, cell.Runs);
            }
        }
    }

    private static void AddContinuationValue(Section section, string value)
    {
        var paragraph = section.AddParagraph(value);
        paragraph.Format.Font.Size = 9;
        paragraph.Format.SpaceAfter = 5;
    }

    private static void RenderBlock(DocumentElements elements, Unit availableWidth, FormDocumentBlock block, IReadOnlyList<FormField> fields, JsonElement data, bool includeEmpty)
    {
        switch (block.Type)
        {
            case "text":
                var paragraph = elements.AddParagraph();
                ApplyStyle(paragraph, block.Style);
                AddText(paragraph, block.Text, block.Runs);
                paragraph.Format.SpaceAfter = Unit.FromPoint(4);
                break;
            case "field":
                var field = fields.FirstOrDefault(item => item.Key == block.FieldKey);
                if (field is not null && TryDisplay(field, data, includeEmpty, out var value))
                    FieldLine(elements, availableWidth, field.Label, value, block.Style);
                break;
            case "table":
                RenderDesignedTable(elements, availableWidth, block, fields, data, includeEmpty);
                break;
        }
    }

    private static void RenderDesignedTable(DocumentElements elements, Unit availableWidth, FormDocumentBlock block, IReadOnlyList<FormField> fields, JsonElement data, bool includeEmpty)
    {
        var table = elements.AddTable();
        table.Rows.LeftIndent = 0;
        table.Borders.Color = Color.Parse(block.BorderColor ?? "#94A3B8");
        table.Borders.Width = Unit.FromPoint((double)block.BorderWidth);
        var widths = block.ColumnWidths ?? Enumerable.Repeat(1m, block.Columns).ToArray();
        var totalWidth = widths.Sum();
        for (var column = 0; column < block.Columns; column++) table.AddColumn(availableWidth * (double)(widths[column] / totalWidth));
        var covered = new HashSet<(int Row, int Column)>();
        for (var rowIndex = 0; rowIndex < block.Rows; rowIndex++) table.AddRow().HeadingFormat = rowIndex < block.HeaderRows;
        for (var rowIndex = 0; rowIndex < block.Rows; rowIndex++)
        {
            var row = table.Rows[rowIndex];
            for (var columnIndex = 0; columnIndex < block.Columns; columnIndex++)
            {
                if (covered.Contains((rowIndex, columnIndex))) continue;
                var model = block.Cells!.Single(item => item.Row == rowIndex && item.Column == columnIndex);
                var cell = row.Cells[columnIndex];
                cell.MergeRight = model.ColSpan - 1;
                cell.MergeDown = model.RowSpan - 1;
                for (var rowOffset = 0; rowOffset < model.RowSpan; rowOffset++)
                    for (var columnOffset = 0; columnOffset < model.ColSpan; columnOffset++)
                        if (rowOffset != 0 || columnOffset != 0) covered.Add((rowIndex + rowOffset, columnIndex + columnOffset));
                cell.VerticalAlignment = VerticalAlignment.Center;
                cell.Shading.Color = Color.Parse(model.BackgroundColor ?? "#FFFFFF");
                var paragraph = cell.AddParagraph();
                ApplyStyle(paragraph, model.Style);
                if (!string.IsNullOrWhiteSpace(model.FieldKey))
                {
                    var field = fields.FirstOrDefault(item => item.Key == model.FieldKey);
                    var content = field is null || !TryDisplay(field, data, includeEmpty, out var value)
                        ? string.Empty : $"{field.Label}\n{value}";
                    paragraph.AddText(content);
                }
                else AddText(paragraph, model.Text, model.Runs);
            }
        }
        table.Format.SpaceAfter = Unit.FromPoint(5);
    }

    private static void FieldLine(DocumentElements elements, Unit availableWidth, string label, string value, FormTextStyle? style)
    {
        var table = elements.AddTable();
        table.Rows.LeftIndent = 0;
        table.Borders.Color = Color.Parse("#CBD9D7");
        table.Borders.Width = .5;
        table.AddColumn(availableWidth * .34);
        table.AddColumn(availableWidth * .66);
        var row = table.AddRow();
        var labelParagraph = row.Cells[0].AddParagraph(label);
        ApplyStyle(labelParagraph, style);
        labelParagraph.Format.Font.Bold = true;
        var valueParagraph = row.Cells[1].AddParagraph(value);
        valueParagraph.Format.Font.Size = Unit.FromPoint(9);
        row.Cells[0].VerticalAlignment = row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
        table.Format.SpaceAfter = Unit.FromPoint(3);
    }

    private static bool TryDisplay(FormField field, JsonElement data, bool includeEmpty, out string value)
    {
        var hasValue = data.TryGetProperty(field.Key, out var raw)
            && raw.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            && !(raw.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(raw.GetString()))
            && !(raw.ValueKind == JsonValueKind.Array && raw.GetArrayLength() == 0);
        value = hasValue ? Display(field, raw) : "-";
        return hasValue || includeEmpty;
    }

    private static void ApplyStyle(Paragraph paragraph, FormTextStyle? style)
    {
        if (style is null) return;
        ApplyFont(paragraph.Format.Font, style);
        paragraph.Format.Alignment = style.Alignment switch
        {
            "center" => ParagraphAlignment.Center,
            "right" => ParagraphAlignment.Right,
            "justify" => ParagraphAlignment.Justify,
            _ => ParagraphAlignment.Left,
        };
        if (!string.Equals(style.BackgroundColor, "#ffffff", StringComparison.OrdinalIgnoreCase))
            paragraph.Format.Shading.Color = Color.Parse(style.BackgroundColor);
    }

    private static void AddText(Paragraph paragraph, string? text, IReadOnlyList<FormTextRun>? runs)
    {
        if (runs is null)
        {
            paragraph.AddText(text ?? string.Empty);
            return;
        }
        foreach (var run in runs) ApplyFont(paragraph.AddFormattedText(run.Text).Font, run.Style);
    }

    private static void ApplyFont(Font font, FormTextStyle style)
    {
        font.Name = PdfFont(style.FontFamily);
        font.Size = Unit.FromPoint((double)style.FontSize);
        font.Bold = style.Bold;
        font.Italic = style.Italic;
        font.Underline = style.Underline ? Underline.Single : Underline.None;
        font.Color = Color.Parse(style.Color);
    }

    private static string PdfFont(string? family) => family is "Georgia" or "Times New Roman" ? "QmsDejaVuSerif" : FontFamily;

    private static string OptionLabel(FormField field, string value) =>
        field.Options.FirstOrDefault(item => item.Value == value)?.Label ?? value;

    private static string Text(JsonElement config, string name, string fallback) =>
        config.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;

    private static bool Flag(JsonElement config, string name, bool fallback) =>
        config.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;

    private static void Brand(Section section, string value, string color)
    {
        var paragraph = section.AddParagraph(value);
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Color.Parse(color);
    }

    private static void Heading(Section section, string value, string color)
    {
        var paragraph = section.AddParagraph(value);
        paragraph.Format.Font.Size = 13;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Color.Parse(color);
        paragraph.Format.SpaceBefore = 9;
        paragraph.Format.SpaceAfter = 4;
    }

    private static Table Table(Section section, string[] headings)
    {
        var table = section.AddTable();
        table.Borders.Color = Color.Parse("#CBD9D7");
        table.Borders.Width = .5;
        foreach (var _ in headings) table.AddColumn(Unit.FromCentimeter(17.4 / headings.Length));
        var row = table.AddRow();
        row.HeadingFormat = true;
        row.Shading.Color = Color.Parse("#EAF4F2");
        for (var index = 0; index < headings.Length; index++) Cell(row.Cells[index], headings[index], true);
        return table;
    }

    private static void Row(Table table, params string[] values)
    {
        var row = table.AddRow();
        for (var index = 0; index < values.Length; index++) Cell(row.Cells[index], values[index]);
    }

    private static void Cell(Cell cell, string value, bool bold = false)
    {
        var paragraph = cell.AddParagraph(value);
        paragraph.Format.Font.Bold = bold;
        paragraph.Format.Font.Size = 8;
        cell.VerticalAlignment = VerticalAlignment.Center;
    }

    private static string EventLabel(string value) => value switch
    {
        "ElectronicFormRecordCreated" => "Kayıt oluşturuldu",
        "ElectronicFormRecordDraftUpdated" => "Taslak güncellendi",
        "ElectronicFormRecordSubmitted" => "İncelemeye gönderildi",
        "ElectronicFormRecordApproved" => "Onaylandı ve kapatıldı",
        _ => value,
    };

    private static string SnapshotHash(ElectronicFormRecordDetailsResponse details) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(details)));

    private static string Format(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(3)).ToString("dd.MM.yyyy HH:mm 'TRT'");

    private static string SafeFileComponent(string value) => string.Concat(value.Select(character =>
        char.IsLetterOrDigit(character) || character is '-' or '_' or '.' ? character : '_'));

    private static void EnsureFont()
    {
        if (GlobalFontSettings.FontResolver is not null) return;
        lock (FontLock) GlobalFontSettings.FontResolver ??= new Resolver();
    }

    private sealed class Resolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            var (linuxName, macName, windowsName) = faceName switch
            {
                "QmsSansBold" => ("DejaVuSans-Bold.ttf", "Arial Bold.ttf", "arialbd.ttf"),
                "QmsSansItalic" => ("DejaVuSans-Oblique.ttf", "Arial Italic.ttf", "ariali.ttf"),
                "QmsSansBoldItalic" => ("DejaVuSans-BoldOblique.ttf", "Arial Bold Italic.ttf", "arialbi.ttf"),
                "QmsSerif" => ("DejaVuSerif.ttf", "Times New Roman.ttf", "times.ttf"),
                "QmsSerifBold" => ("DejaVuSerif-Bold.ttf", "Times New Roman Bold.ttf", "timesbd.ttf"),
                "QmsSerifItalic" => ("DejaVuSerif-Italic.ttf", "Times New Roman Italic.ttf", "timesi.ttf"),
                "QmsSerifBoldItalic" => ("DejaVuSerif-BoldItalic.ttf", "Times New Roman Bold Italic.ttf", "timesbi.ttf"),
                _ => ("DejaVuSans.ttf", "Arial.ttf", "arial.ttf"),
            };
            var candidates = new[]
            {
                Path.Combine("/usr/share/fonts/truetype/dejavu", linuxName),
                Path.Combine("/System/Library/Fonts/Supplemental", macName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), windowsName),
            };
            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("PDF üretimi için DejaVu Sans/Serif veya Arial/Times New Roman yazı tipleri kurulmalıdır.");
            return File.ReadAllBytes(path);
        }
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        {
            var prefix = familyName == "QmsDejaVuSerif" ? "QmsSerif" : "QmsSans";
            var face = bold && italic ? prefix + "BoldItalic" : bold ? prefix + "Bold" : italic ? prefix + "Italic" : prefix;
            return new FontResolverInfo(face, false, false);
        }
    }
}
