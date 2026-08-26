using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Qms.Contracts.ChangeControls;
using Qms.Infrastructure.ChangeControls;

namespace Qms.IntegrationTests;

public sealed class ChangeControlFinalReportTests
{
    [Fact]
    public async Task Closed_change_control_produces_immutable_visual_qa_pdf()
    {
        // Production images install this font explicitly. Minimal SDK test
        // images may omit it; visual QA runs in the font-enabled container.
        if (!File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")) return;
        var root = Path.Combine(Path.GetTempPath(), "qms-m03-pdf-tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = root }).Build();
        var now = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero); var recordId = Guid.NewGuid(); var qualityRecordId = Guid.NewGuid(); var ownerId = Guid.NewGuid();
        var record = new ChangeControlResponse(recordId, qualityRecordId, "DK-2026-000999", null, null, "Bilgisayarlı Sistem", "Dolum hattı sıcaklık izleme değişikliği", "Manuel sıcaklık kayıtları ve vardiya sonu kontrolü", "Otomatik sensör, alarm ve değiştirilemez elektronik trend kaydı", "Veri bütünlüğünü ve erken uyarıyı güçlendirmek", "Dolum hattı, ilgili SOP, eğitim ve validasyon", false, null, ownerId, "Mert Kaya", now.AddDays(30), "Yüksek", "Ürün kalitesi ve veri bütünlüğü etkileri değerlendirildi.", true, true, true, "Notification", "Önceki onaylı PLC reçetesi ve manuel forma kontrollü dönüş", "RUH-2026-184", now.AddDays(10), "İki haftalık izleme sonunda alarm ve trend kontrolleri başarılı.", "Tüm bağımlılıklar, eğitimler ve validasyon çıktıları kapatıldı.", "Closed", now.AddDays(-30), now.AddDays(14), now.AddDays(14), 18);
        var assessments = new[] { new ChangeAssessmentResponse(Guid.NewGuid(), Guid.NewGuid(), "Üretim", Guid.NewGuid(), "Ayşe Demir", "Approved", "Operasyon akışında kontrollü duruş gereklidir.", "SOP revizyonu ve operatör eğitimi", now.AddDays(-20)), new ChangeAssessmentResponse(Guid.NewGuid(), Guid.NewGuid(), "Kalite Güvence", Guid.NewGuid(), "Elif Yılmaz", "Approved", "Validasyon ve veri bütünlüğü kontrolleri uygundur.", "IQ/OQ/PQ raporlarının onayı", now.AddDays(-19)) };
        var actions = new[] { new ChangeActionResponse(Guid.NewGuid(), "Validasyon", "Sensör IQ/OQ/PQ protokolünü tamamla ve onaylı raporu arşivle", ownerId, "Mert Kaya", now.AddDays(5), true, "Verified", "VAL-2026-044 raporu", "Kanıt ve sonuçlar KG tarafından doğrulandı.", now.AddDays(4), now.AddDays(5)), new ChangeActionResponse(Guid.NewGuid(), "Eğitim", "Dolum operatörlerine yeni alarm yönetimi eğitimi ver", Guid.NewGuid(), "Zeynep Arslan", now.AddDays(6), true, "Verified", "EGT-2026-091 katılım kayıtları", "Katılım ve değerlendirme sonuçları uygun.", now.AddDays(5), now.AddDays(6)) };
        var audit = new[] { new ChangeAuditEventResponse(Guid.NewGuid(), 1, "ChangeControlCreated", "Okan Çelik", now.AddDays(-30), null, JsonDocument.Parse("{}").RootElement.Clone()), new ChangeAuditEventResponse(Guid.NewGuid(), 18, "ChangeControlStatusChanged", "Elif Yılmaz", now.AddDays(14), "Nihai kapanış", JsonDocument.Parse("{}").RootElement.Clone()) };
        var signatures = new[] { new ChangeSignatureResponse(Guid.NewGuid(), 8, Guid.NewGuid(), "Ayşe Demir", "Değişiklik kurulu kararı", now.AddDays(-15), new string('a', 64), "Kurul uygun buldu."), new ChangeSignatureResponse(Guid.NewGuid(), 18, Guid.NewGuid(), "Elif Yılmaz", "Değişiklik nihai kapanış onayı", now.AddDays(14), new string('b', 64), "Kapanış uygun.") };
        var details = new ChangeControlDetailsResponse(record, assessments, actions, audit, signatures, [], []);
        var result = await new ChangeControlFinalReportService(configuration).EnsureGeneratedAsync(details, CancellationToken.None);
        Assert.True(result.Content.Length > 5_000); Assert.Equal(64, result.Sha256.Length);
        var output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../output/pdf/M03-change-control-visual-qa.pdf")); Directory.CreateDirectory(Path.GetDirectoryName(output)!); await File.WriteAllBytesAsync(output, result.Content);
    }
}
