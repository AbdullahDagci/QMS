using Qms.Domain.ChangeControls;

namespace Qms.Domain.Tests.ChangeControls;

public sealed class ChangeControlTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Board_Review_Waits_For_All_Parallel_Assessments()
    {
        var change = Create("None");
        change.Transition(1, "submit", null, true, Now.AddMinutes(1));
        change.Transition(2, "approve-preliminary", null, true, Now.AddMinutes(2));
        var first = change.Assessments.First();
        change.CompleteAssessment(3, first.Id, true, "Etki değerlendirildi.", "Doküman revizyonu", Now.AddMinutes(3));

        var error = Assert.Throws<InvalidOperationException>(() => change.Transition(4, "submit-board", null, true, Now.AddMinutes(4)));
        Assert.Contains("Tüm paralel", error.Message);
    }

    [Fact]
    public void Authority_Approval_And_Verified_Actions_Are_Required_For_Commissioning()
    {
        var change = MoveToImplementation("AuthorityApproval");
        var action = Assert.Single(change.Actions);
        change.RequestActionCompletion(10, action.Id, "Validasyon raporu eklendi.", Now.AddMinutes(10));
        change.VerifyAction(11, action.Id, true, "Kanıt doğrulandı.", Now.AddMinutes(11));
        Assert.Throws<InvalidOperationException>(() => change.Transition(12, "request-commissioning", null, true, Now.AddMinutes(12)));
        change.SetAuthorityApproval(12, "TR-VAR-2026-184.pdf", Now.AddMinutes(12));
        change.Transition(13, "request-commissioning", null, true, Now.AddMinutes(13));
        Assert.Equal(ChangeControlStatus.CommissioningApproval, change.Status);
    }

    [Fact]
    public void Commissioning_Is_Separate_From_Final_Closure()
    {
        var change = MoveToImplementation("None");
        var action = Assert.Single(change.Actions);
        change.RequestActionCompletion(9, action.Id, "Uygulama kanıtı", Now.AddMinutes(9));
        change.VerifyAction(10, action.Id, true, "Doğrulandı", Now.AddMinutes(10));
        change.Transition(11, "request-commissioning", null, true, Now.AddMinutes(11));
        change.Transition(12, "commission", "Devreye alma kontrolleri uygun.", true, Now.AddMinutes(12));
        Assert.Equal(ChangeControlStatus.PostImplementationVerification, change.Status);
        Assert.NotNull(change.CommissionedAtUtc);
        Assert.Null(change.ClosedAtUtc);
        change.Transition(13, "verify-implementation", "İzleme başarılı.", true, Now.AddDays(2));
        change.Transition(14, "close", "Tüm bağımlılıklar kapandı.", true, Now.AddDays(2).AddMinutes(1));
        Assert.Equal(ChangeControlStatus.Closed, change.Status);
    }

    private static ChangeControl MoveToImplementation(string regulatoryImpact)
    {
        var change = Create(regulatoryImpact);
        change.Transition(1, "submit", null, true, Now.AddMinutes(1));
        change.Transition(2, "approve-preliminary", null, true, Now.AddMinutes(2));
        var version = 3L;
        foreach (var assessment in change.Assessments)
        {
            change.CompleteAssessment(version++, assessment.Id, true, "Etki kabul edilebilir.", "Aksiyon planlandı.", Now.AddMinutes(version));
        }
        change.Transition(version++, "submit-board", null, true, Now.AddMinutes(version));
        change.Transition(version++, "approve-board", "Kurul onayı", true, Now.AddMinutes(version));
        change.AddAction(version++, "Validasyon", "Hat uygunluk testini tamamla", "Validasyon Ekibi", Now.AddDays(10), true, Now.AddMinutes(version));
        change.Transition(version, "approve-plan", "Plan uygun", true, Now.AddMinutes(version));
        return change;
    }

    private static ChangeControl Create(string regulatoryImpact) => ChangeControl.Create(
        Guid.NewGuid(), Guid.NewGuid(), "Proses", "Dolum sıcaklık alarmı değişikliği", "Alarm 28°C", "Alarm 25°C", "Sapma tekrarını önlemek", "Dolum hattı ve ilgili SOP", false, null, "Kalite Güvence", Now.AddDays(30), "Yüksek", "Ürün kalitesi etkilenebilir.", true, false, true, regulatoryImpact, "Önceki PLC reçetesini geri yükle", ["Üretim"], Now);
}
