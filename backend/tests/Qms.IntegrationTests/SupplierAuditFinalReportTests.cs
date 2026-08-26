using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Qms.Contracts.SupplierAudits;
using Qms.Infrastructure.SupplierAudits;

namespace Qms.IntegrationTests;

public sealed class SupplierAuditFinalReportTests
{
    [Fact]
    public async Task Closed_supplier_audit_produces_immutable_pdf_with_signatures()
    {
        if (!File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")) return;
        var root = Path.Combine(Path.GetTempPath(), "qms-m09-pdf-tests", Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "FileStorage:RootPath", root } }).Build();
        var now = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero); var id = Guid.NewGuid(); var qr = Guid.NewGuid();
        var record = new SupplierAuditRecordResponse(id, qr, null, "TD-2026-000999", "TED-999", "Biyo Ambalaj Ltd.", "Primer ambalaj", "Steril kapak", "Türkiye", "Orta", 88, 0, 29, "Düşük", 36, "GMP kalite sistemi", "Ankara", Guid.NewGuid(), "Elif Yılmaz", "Kalite Güvence", Guid.NewGuid(), "Hakan Özkan", Guid.NewGuid(), "Dr. Aylin Kurt", Guid.NewGuid(), "Zeynep Demir", now.AddDays(-15), now.AddDays(-13), "SA-2026.2", now.AddDays(-15), "Approved", "Denetim kanıtları uygun.", now.AddYears(2), false, "Closed", now.AddDays(-20), now, now, 12);
        var checklist = new[] { new SupplierAuditChecklistResponse(Guid.NewGuid(), 1, "Kalite Sistemleri", "Değişiklik bildirimi etkin mi?", "GMP Bölüm 5", "Conform", "Kayıt örneklemi", "Uygun", now.AddDays(-12)) };
        var events = new[] { new SupplierAuditEventResponse(Guid.NewGuid(), 12, "SupplierAuditStatusChanged", "Zeynep Demir", now, "Nihai kapanış", JsonDocument.Parse("{}").RootElement.Clone()) };
        var signatures = new[] { new SupplierAuditSignatureResponse(Guid.NewGuid(), 11, Guid.NewGuid(), "Zeynep Demir", "Tedarikçi nitelendirme kararı", now.AddMinutes(-1), new string('a', 64), null), new SupplierAuditSignatureResponse(Guid.NewGuid(), 12, Guid.NewGuid(), "Zeynep Demir", "Tedarikçi denetimi nihai kapanış onayı", now, new string('b', 64), "Nihai kapanış") };
        var details = new SupplierAuditDetailsResponse(record, checklist, [], [], events, [], signatures); var service = new SupplierAuditFinalReportService(config);
        var first = await service.EnsureGeneratedAsync(details, CancellationToken.None); var second = await service.EnsureGeneratedAsync(details, CancellationToken.None);
        Assert.True(first.Content.Length > 5000); Assert.Equal(first.Sha256, second.Sha256); Assert.Equal(first.Content, second.Content);
    }
}
