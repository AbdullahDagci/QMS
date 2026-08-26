using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Qms.Contracts.ExternalAudits;
using Qms.Infrastructure.ExternalAudits;

namespace Qms.IntegrationTests;

public sealed class ExternalAuditFinalReportTests
{
    [Fact]
    public async Task Closed_external_audit_produces_immutable_pdf_with_signatures()
    {
        if(!File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"))return;
        var root=Path.Combine(Path.GetTempPath(),"qms-m08-pdf-tests",Guid.NewGuid().ToString("N"));
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"FileStorage:RootPath",root}}).Build();
        var now=new DateTimeOffset(2026,8,26,10,0,0,TimeSpan.Zero);var id=Guid.NewGuid();var qr=Guid.NewGuid();
        var record=new ExternalAuditRecordResponse(id,qr,"DD-2026-000999","TİTCK GMP denetimi","Otorite denetimi","TİTCK",true,"Türkiye","TİTCK-2026-99","Üretim ve kalite sistemleri","İstanbul Üretim Tesisi","Elif Yılmaz",now.AddDays(-20),now.AddDays(-15),now.AddDays(-13),now.AddDays(-5),Guid.NewGuid(),"Dr. Aylin Kurt","TİTCK-KAP-99","Kapanış mektubu sisteme yüklendi",true,now.AddDays(-1),"Tüm taahhütler doğrulandı","Closed",now.AddDays(-20),now,now,18);
        var docs=new[]{new ExternalAuditDocumentResponse(Guid.NewGuid(),null,"SOP-QA-001","Kalite Sistemleri Prosedürü","Kurum İçi","Exported",1,now.AddDays(-16))};
        var accesses=new[]{new ExternalAuditPackageAccessResponse(Guid.NewGuid(),docs[0].Id,"Elif Yılmaz","TİTCK","Denetim talep paketi","Güvenli aktarım kanıtı",1,now.AddDays(-16))};
        var findings=new[]{new ExternalAuditFindingResponse(Guid.NewGuid(),"DBG-2026-000999","Erişim gözlemi","Pasif hesap açık bulundu","GMP 4.10","Minor",false,null,null,"Hakan Özkan",now.AddDays(-8),"Kabul edildi","Hesap kapatıldı",now.AddDays(-6),"Kanıt doğrulandı","Closed",now.AddDays(-13),now.AddDays(-2))};
        var audit=new[]{new ExternalAuditEventResponse(Guid.NewGuid(),18,"ExternalAuditStatusChanged","Dr. Aylin Kurt",now,"Nihai kapanış",JsonDocument.Parse("{}").RootElement.Clone())};
        var signs=new[]{new ExternalAuditSignatureResponse(Guid.NewGuid(),16,Guid.NewGuid(),"Dr. Aylin Kurt","Dış denetim kapanış mektubu ve otorite kabul doğrulaması",now.AddDays(-1),new string('a',64),null),new ExternalAuditSignatureResponse(Guid.NewGuid(),18,Guid.NewGuid(),"Dr. Aylin Kurt","Dış denetim nihai kapanış onayı",now,new string('b',64),"Nihai kapanış")};
        var details=new ExternalAuditDetailsResponse(record,docs,accesses,findings,audit,signs,[],[]);var service=new ExternalAuditFinalReportService(config);
        var first=await service.EnsureGeneratedAsync(details,CancellationToken.None);var second=await service.EnsureGeneratedAsync(details,CancellationToken.None);
        Assert.True(first.Content.Length>5000);Assert.Equal(first.Sha256,second.Sha256);Assert.Equal(first.Content,second.Content);
    }
}
