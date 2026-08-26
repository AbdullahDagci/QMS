using Qms.Domain.InternalAudits;

namespace Qms.Domain.Tests.InternalAudits;

public sealed class InternalAuditTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Auditor_Must_Be_Independent_From_Auditee_Department()
    {
        var audit = Create("Üretim", "Üretim");
        audit.Transition(1, "prepare", null, Now);

        var error = Assert.Throws<InvalidOperationException>(() => audit.Transition(2, "submit-plan", null, Now));

        Assert.Contains("bağımsız", error.Message);
        Assert.False(audit.IndependenceConfirmed);
    }

    [Fact]
    public void Checklist_Must_Be_Complete_Before_Execution_Ends()
    {
        var audit = Create();
        audit.Transition(1, "prepare", null, Now);
        audit.Transition(2, "submit-plan", null, Now);
        audit.Transition(3, "approve-plan", null, Now);

        Assert.NotNull(audit.ChecklistLockedAtUtc);
        Assert.Throws<InvalidOperationException>(() => audit.Transition(4, "complete-execution", null, Now));
        audit.Answer(4, audit.Checklist.Single().Id, AuditAnswerStatus.Conform, "BPR-260825", "Kayıt örneklemi doğrulandı.", Now);
        audit.Transition(5, "complete-execution", null, Now);

        Assert.Equal(InternalAuditStatus.Findings, audit.Status);
    }

    [Fact]
    public void Open_Finding_And_Open_Capa_Block_Audit_Closure()
    {
        var audit = Create();
        audit.Transition(1, "prepare", null, Now); audit.Transition(2, "submit-plan", null, Now); audit.Transition(3, "approve-plan", null, Now);
        audit.Answer(4, audit.Checklist.Single().Id, AuditAnswerStatus.Nonconform, "FRM-YET-04", "Matris güncel değil.", Now);
        var finding = AuditFinding.Create(audit.Id, "BLG-2026-000001", "Yetki matrisi güncel değil", "Pozisyon değişikliği matrise yansımamış.", "SOP-IK-04", 4, 4, true, Guid.NewGuid(), "İK Sorumlusu", Now.AddDays(30), Now);
        audit.AddFinding(5, finding, Now); audit.Transition(6, "complete-execution", null, Now); audit.Transition(7, "confirm-findings", null, Now);
        audit.RespondFinding(8, finding.Id, "Gecikme doğrulandı.", "Matris revize edilip eğitim verilecek.", Now); audit.Transition(9, "complete-responses", null, Now);

        Assert.Throws<InvalidOperationException>(() => audit.CloseFinding(10, finding.Id, "Kanıt uygundur.", false, Now));
        audit.CloseFinding(10, finding.Id, "DÖF ve etkinlik kanıtı doğrulandı.", true, Now);
        audit.Transition(11, "start-finding-closure", null, Now); audit.Transition(12, "request-audit-closure", null, Now); audit.Transition(13, "close", "Tüm denetim koşulları tamamlandı.", Now);

        Assert.Equal(InternalAuditStatus.Closed, audit.Status);
    }

    private static InternalAudit Create(string auditee = "Üretim", string auditorDepartment = "Kalite Güvence") =>
        InternalAudit.Create(Guid.NewGuid(), 2026, "Üretim kayıtları iç denetimi", "Proses denetimi", "Batch kayıtları", "Veri bütünlüğünü doğrulamak", "ISO 9001", Guid.NewGuid(), auditee, Guid.NewGuid(), "Ayşe Denetçi", auditorDepartment, Now.AddDays(1), Now.AddDays(2), false, null, "2026.1", [("Batch kaydı izlenebilir mi?", "ISO 9001 7.5")], Now);
}
