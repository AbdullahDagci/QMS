using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Qms.Contracts.Complaints;
using Qms.Infrastructure.Complaints;

namespace Qms.IntegrationTests;

public sealed class ComplaintFinalReportTests
{
    [Fact]
    public async Task Closed_complaint_produces_immutable_pdf_with_signatures()
    {
        if (!File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")) return;
        var root = Path.Combine(Path.GetTempPath(), "qms-m06-pdf-tests", Guid.NewGuid().ToString("N")); var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = root }).Build(); var now = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero); var id = Guid.NewGuid(); var qualityRecordId = Guid.NewGuid(); var ownerId = Guid.NewGuid();
        var record = new ComplaintRecordResponse(id, qualityRecordId, "ŞK-2026-000999", "Email", "Anadolu Ecza Deposu", "TR", "QMS Tablet 10 mg", "B260801", now.AddDays(-20), now.AddDays(-19), "ProductQuality", "Blister içinde kırık tablet bildirildi.", "Major", false, false, true, true, "Fotoğraf ve iade numunesi", ownerId, "Elif Yılmaz", now.AddDays(-16), now.AddDays(-2), 2, true, Guid.NewGuid(), "SP-2026-000099", Guid.NewGuid(), "DÖF-2026-000088", null, null, null, "Tek batch etkilenmiştir; dağıtım karantinaya alınmıştır.", "Blister besleme kılavuzu aşınması", true, "Nihai yanıt müşteriyle paylaşılmış ve kabul edilmiştir.", "Closed", now.AddDays(-19), now, now, 14);
        var investigations = new[] { new ComplaintInvestigationResponse(Guid.NewGuid(), Guid.NewGuid(), "Üretim", Guid.NewGuid(), "Mert Kaya", "Completed", "Batch kayıtları ve hat parametreleri incelendi.", "Besleme kılavuzu tolerans dışıdır.", now.AddDays(-8)), new ComplaintInvestigationResponse(Guid.NewGuid(), Guid.NewGuid(), "Kalite Kontrol", Guid.NewGuid(), "Selin Aksoy", "Completed", "Şahit numune uygun, iade numunesi kırık bulundu.", "Paketleme sırasında mekanik hasar", now.AddDays(-7)) };
        var responses = new[] { new ComplaintResponseVersionResponse(Guid.NewGuid(), "Preliminary", 1, "Bildirim alınmış, ilgili batch kontrol altına alınmıştır.", "Approved", ownerId, "Elif Yılmaz", Guid.NewGuid(), "Okan Çelik", now.AddDays(-18), now.AddDays(-17)), new ComplaintResponseVersionResponse(Guid.NewGuid(), "Final", 1, "Araştırma tamamlanmış, düzeltici faaliyet başlatılmıştır.", "Approved", ownerId, "Elif Yılmaz", Guid.NewGuid(), "Okan Çelik", now.AddDays(-2), now.AddDays(-1)) };
        var audit = new[] { new ComplaintAuditEventResponse(Guid.NewGuid(), 1, "ComplaintCreated", "Elif Yılmaz", now.AddDays(-19), null, JsonDocument.Parse("{}").RootElement.Clone()), new ComplaintAuditEventResponse(Guid.NewGuid(), 14, "ComplaintStatusChanged", "Okan Çelik", now, "Nihai kapanış", JsonDocument.Parse("{}").RootElement.Clone()) };
        var signatures = new[] { new ComplaintSignatureResponse(Guid.NewGuid(), 5, Guid.NewGuid(), "Okan Çelik", "Preliminary müşteri yanıtı onayı", now.AddDays(-17), new string('a', 64), null), new ComplaintSignatureResponse(Guid.NewGuid(), 13, Guid.NewGuid(), "Okan Çelik", "Final müşteri yanıtı onayı", now.AddDays(-1), new string('b', 64), null), new ComplaintSignatureResponse(Guid.NewGuid(), 14, Guid.NewGuid(), "Okan Çelik", "Müşteri şikâyeti nihai kapanış onayı", now, new string('c', 64), "Nihai kapanış") };
        var details = new ComplaintDetailsResponse(record, investigations, responses, audit, signatures, [], []); var service = new ComplaintFinalReportService(config); var first = await service.EnsureGeneratedAsync(details, CancellationToken.None); var second = await service.EnsureGeneratedAsync(details, CancellationToken.None);
        Assert.True(first.Content.Length > 5000); Assert.Equal(first.Sha256, second.Sha256); Assert.Equal(first.Content, second.Content);
    }
}
