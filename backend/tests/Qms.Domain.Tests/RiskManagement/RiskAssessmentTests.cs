using Qms.Domain.RiskManagement;
namespace Qms.Domain.Tests.RiskManagement;public sealed class RiskAssessmentTests
{
 [Fact]public void High_risk_requires_action_and_residual_below_threshold(){var now=DateTimeOffset.UtcNow;var risk=RiskAssessment.Create(Guid.NewGuid(),"Dolum","Kapsam","Process","FMEA","v1",40,Guid.NewGuid(),"Sahip",null,null,Guid.NewGuid(),"Onaylayan",now);var item=RiskItem.Create(risk.Id,"Sıcaklık yüksek","Stabilite","Sensör","Alarm",5,3,4,"Kalibre et",Guid.NewGuid(),"Emre",now.AddDays(3),40,now);risk.AddItem(1,item,now);risk.StartScoring(2,now);risk.CompleteScoring(3,now);risk.CompleteAction(4,item.Id,"Sertifika",now);risk.StartResidualReview(5,now);risk.SetResidual(6,item.Id,5,1,2,"Kontroller yeterli",now);risk.Approve(7,now);risk.Close(8,now);Assert.Equal(RiskAssessmentStatus.Closed,risk.Status);Assert.Equal(10,item.ResidualRpn);}
 [Fact]public void High_risk_without_action_is_rejected(){var now=DateTimeOffset.UtcNow;Assert.Throws<ArgumentException>(()=>RiskItem.Create(Guid.NewGuid(),"Hata","Etki","Neden","Kontrol",5,4,3,null,null,null,null,40,now));}
}
