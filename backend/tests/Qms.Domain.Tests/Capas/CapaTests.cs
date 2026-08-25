using Qms.Domain.Capas;

namespace Qms.Domain.Tests.Capas;

public sealed class CapaTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_Cannot_Be_Submitted_Without_Action()
    {
        var capa = Create(false);
        capa.Transition(1, "submit", null, false, Now.AddMinutes(1));
        capa.Transition(2, "approve-scope", null, false, Now.AddMinutes(2));
        capa.Transition(3, "approve-root-cause", null, false, Now.AddMinutes(3));

        var error = Assert.Throws<InvalidOperationException>(() => capa.Transition(4, "submit-plan", null, false, Now.AddMinutes(4)));
        Assert.Contains("En az bir", error.Message);
    }

    [Fact]
    public void Owner_Completion_Requires_Quality_Verification_Before_Closure()
    {
        var capa = MoveToImplementation(false);
        var action = Assert.Single(capa.Actions);
        capa.RequestActionCompletion(7, action.Id, "Kalibrasyon sertifikası eklendi.", Now.AddMinutes(7));
        capa.Transition(8, "request-action-verification", null, false, Now.AddMinutes(8));
        Assert.Throws<InvalidOperationException>(() => capa.Transition(9, "approve-actions", null, false, Now.AddMinutes(9)));
        capa.VerifyAction(9, action.Id, true, "Kanıt KG tarafından doğrulandı.", Now.AddMinutes(9));
        capa.Transition(10, "approve-actions", null, false, Now.AddMinutes(10));
        Assert.Equal(CapaStatus.ClosureApproval, capa.Status);
        capa.Transition(11, "close", "Tüm koşullar sağlandı.", false, Now.AddMinutes(11));
        Assert.Equal(CapaStatus.Closed, capa.Status);
    }

    [Fact]
    public void Ineffective_Result_Returns_To_Action_Planning()
    {
        var capa = MoveToImplementation(true);
        var action = Assert.Single(capa.Actions);
        capa.RequestActionCompletion(7, action.Id, "Kanıt", Now.AddMinutes(7));
        capa.Transition(8, "request-action-verification", null, false, Now.AddMinutes(8));
        capa.VerifyAction(9, action.Id, true, "Doğrulandı", Now.AddMinutes(9));
        capa.Transition(10, "approve-actions", null, false, Now.AddMinutes(10));
        capa.Transition(11, "start-effectiveness-review", null, false, Now.AddDays(2));
        capa.Transition(12, "complete-effectiveness", "Hedeflenen düşüş sağlanmadı.", false, Now.AddDays(2).AddMinutes(1));
        Assert.Equal(CapaStatus.ActionPlanning, capa.Status);
        Assert.False(capa.IsEffective);
    }

    private static Capa MoveToImplementation(bool effectiveness)
    {
        var capa = Create(effectiveness);
        capa.Transition(1, "submit", null, false, Now.AddMinutes(1));
        capa.Transition(2, "approve-scope", null, false, Now.AddMinutes(2));
        capa.Transition(3, "approve-root-cause", null, false, Now.AddMinutes(3));
        capa.AddAction(4, "Düzeltici", "Ekipmanı yeniden kalibre et", "Bakım", Now.AddDays(10), Now.AddMinutes(4));
        capa.Transition(5, "submit-plan", null, false, Now.AddMinutes(5));
        capa.Transition(6, "approve-plan", null, false, Now.AddMinutes(6));
        return capa;
    }

    private static Capa Create(bool effectiveness) => Capa.Create(
        Guid.NewGuid(), Guid.NewGuid(), "Deviation", "Kalibrasyon DÖF", "Ölçüm sapması", "Periyodik kontrol eksikliği", "Ekipman durduruldu", "Kalite", Now.AddDays(30), effectiveness,
        effectiveness ? "Trend analizi" : "", effectiveness ? "30 kayıt" : "", effectiveness ? 1 : 0,
        effectiveness ? "Sapma tekrarı sıfır" : "", effectiveness ? "Kalite Güvence" : "", Now);
}
