using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Qms.Contracts.Documents;
using Qms.Infrastructure.Documents;

namespace Qms.IntegrationTests;

public sealed class DocumentFinalReportTests
{
    [Fact]
    public async Task Archived_document_produces_immutable_visual_qa_pdf()
    {
        if (!File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")) return;
        var root = Path.Combine(Path.GetTempPath(), "qms-m04-pdf-tests", Guid.NewGuid().ToString("N")); var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = root }).Build();
        var now = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero); var recordId = Guid.NewGuid(); var qualityRecordId = Guid.NewGuid(); var revisionId = Guid.NewGuid(); var ownerId = Guid.NewGuid(); var departmentId = Guid.NewGuid();
        var record = new ControlledDocumentResponse(recordId, qualityRecordId, "DOC-2026-000999", null, null, "SOP-PRD-014", "Dolum hattı sıcaklık izleme prosedürü", "SOP", ownerId, "Mert Kaya", departmentId, "Üretim", "Internal", 12, now.AddDays(-30), now.AddMonths(12), revisionId, "1.0", "Archived", "Yeni proses tasarımı nedeniyle yürürlükten kaldırıldı.", now.AddDays(-60), now, now, 14);
        var revisions = new[] { new DocumentRevisionResponse(revisionId, "1.0", "Amaç: Dolum hattı sıcaklığının kontrollü izlenmesi.\nSorumluluk: Üretim ve Kalite Güvence.\nUygulama: Alarm limitleri vardiya başlangıcında doğrulanır; sapmalar M.01 sürecine aktarılır.", "İlk kontrollü yayın", "Mert Kaya", "Archived", now.AddDays(-60), now.AddDays(-45), now.AddDays(-40), true) };
        var reviews = new[] { new DocumentReviewResponse(Guid.NewGuid(), revisionId, departmentId, "Üretim", Guid.NewGuid(), "Ayşe Demir", "Approved", "Operasyon adımları uygulanabilir.", now.AddDays(-50)), new DocumentReviewResponse(Guid.NewGuid(), revisionId, Guid.NewGuid(), "Kalite Güvence", Guid.NewGuid(), "Elif Yılmaz", "Approved", "GMP ve veri bütünlüğü kontrolleri uygundur.", now.AddDays(-49)) };
        var trainings = new[] { new DocumentTrainingResponse(Guid.NewGuid(), revisionId, Guid.NewGuid(), "Dolum Operatörü", "Zeynep Arslan", "Completed", "EGT-2026-091 başarıyla tamamlandı.", now.AddDays(-42)) };
        var copies = new[] { new ControlledCopyResponse(Guid.NewGuid(), revisionId, "KK-001", "Dolum Hattı 1", "Saha kullanım kopyası", "Destroyed", now.AddDays(-40), null, null, now.AddDays(-1)) };
        var receipts = new[] { new DocumentReadReceiptResponse(Guid.NewGuid(), revisionId, Guid.NewGuid(), "Zeynep Arslan", "Dokümanı okudum ve anladım", now.AddDays(-39)) };
        var audit = new[] { new DocumentAuditEventResponse(Guid.NewGuid(), 1, "DocumentCreated", "Okan Çelik", now.AddDays(-60), null, JsonDocument.Parse("{}").RootElement.Clone()), new DocumentAuditEventResponse(Guid.NewGuid(), 14, "DocumentStatusChanged", "Elif Yılmaz", now, "Arşiv onayı", JsonDocument.Parse("{}").RootElement.Clone()) };
        var signatures = new[] { new DocumentSignatureResponse(Guid.NewGuid(), 8, Guid.NewGuid(), "Elif Yılmaz", "Doküman kalite onayı", now.AddDays(-45), new string('a', 64), "İçerik uygundur."), new DocumentSignatureResponse(Guid.NewGuid(), 14, Guid.NewGuid(), "Okan Çelik", "Doküman arşiv onayı", now, new string('b', 64), "Arşivleme uygundur.") };
        var details = new ControlledDocumentDetailsResponse(record, revisions, reviews, trainings, copies, receipts, audit, signatures, [], []);
        var result = await new DocumentFinalReportService(configuration).EnsureGeneratedAsync(details, CancellationToken.None);
        Assert.True(result.Content.Length > 5_000); Assert.Equal(64, result.Sha256.Length);
        var second = await new DocumentFinalReportService(configuration).EnsureGeneratedAsync(details, CancellationToken.None); Assert.Equal(result.Sha256, second.Sha256); Assert.Equal(result.Content, second.Content);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../output/pdf/M04-document-visual-qa.pdf")); Directory.CreateDirectory(Path.GetDirectoryName(output)!); await File.WriteAllBytesAsync(output, result.Content);
    }
}
