using Qms.Domain.SupplierAudits;

namespace Qms.Domain.Tests.SupplierAudits;

public sealed class SupplierAuditTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Risk_Profile_Determines_Band_And_Frequency()
    {
        var audit = Create("Kritik", 70, 2);
        Assert.Equal(68, audit.RiskScore);
        Assert.Equal("Yüksek", audit.RiskBand);
        Assert.Equal(18, audit.RecommendedFrequencyMonths);
    }

    [Fact]
    public void Checklist_Must_Be_Completed_Before_Execution_Ends()
    {
        var audit = Create();
        audit.Transition(1, "define-scope", Now); audit.Transition(2, "assign-auditor", Now); audit.Transition(3, "start-audit", Now);
        Assert.Throws<InvalidOperationException>(() => audit.Transition(4, "complete-audit", Now));
        audit.Answer(4, audit.Checklist.Single().Id, SupplierAuditChecklistStatus.Conform, "CoA-2026-41", "Örneklem doğrulandı.", Now);
        audit.Transition(5, "complete-audit", Now);
        Assert.Equal(SupplierAuditStatus.Findings, audit.Status);
        Assert.NotNull(audit.ChecklistLockedAtUtc);
    }

    [Fact]
    public void Critical_Finding_Suspends_Supplier_And_Open_Capa_Blocks_Result()
    {
        var audit = Create(); audit.Transition(1, "define-scope", Now); audit.Transition(2, "assign-auditor", Now); audit.Transition(3, "start-audit", Now); audit.Answer(4, audit.Checklist.Single().Id, SupplierAuditChecklistStatus.Nonconform, "VAL-04", "Validasyon boşluğu", Now);
        var finding = SupplierAuditFinding.Create(audit.Id, "TDB-2026-000001", "Sterilizasyon validasyonu eksik", "Yeniden validasyon kanıtı sunulamadı.", "GMP Annex 1", SupplierAuditFindingClassification.Critical, true, Guid.NewGuid(), Guid.NewGuid(), "Tedarikçi Kalite Müdürü", Now.AddDays(20), Now);
        audit.AddFinding(5, finding, Now);
        Assert.Equal(SupplierQualificationStatus.Suspended, audit.QualificationStatus);
        audit.Transition(6, "complete-audit", Now); audit.Transition(7, "request-supplier-response", Now); audit.RespondFinding(8, finding.Id, "Bulgu kabul edildi.", "Validasyon tekrar edilecek.", Now.AddDays(15), Now); audit.Transition(9, "start-verification", Now); audit.SubmitFindingEvidence(10, finding.Id, "Onaylı protokol ve ham veri paketi", Now); audit.Transition(11, "start-capa", Now);
        Assert.Throws<InvalidOperationException>(() => audit.CloseFinding(12, finding.Id, "Kanıt doğrulandı.", false, Now));
        audit.CloseFinding(12, finding.Id, "DÖF kapanışı ve etkinlik kanıtı doğrulandı.", true, Now); audit.Transition(13, "record-result", Now);
        Assert.Throws<InvalidOperationException>(() => audit.RecordResult(14, SupplierQualificationStatus.Approved, "Doğrudan onay", Now.AddYears(1), false, Now));
        audit.RecordResult(14, SupplierQualificationStatus.Conditional, "Yeniden nitelendirme tamamlanana kadar koşullu kullanım.", Now.AddMonths(6), true, Now); audit.Transition(15, "close", Now);
        Assert.Equal(SupplierAuditStatus.Closed, audit.Status);
    }

    private static SupplierAudit Create(string criticality = "Yüksek", decimal performance = 80, int openFindings = 1) => SupplierAudit.Create(Guid.NewGuid(), null, "TED-0041", "Medikal Ambalaj A.Ş.", "Primer ambalaj", "Steril flakon tıpası", "Türkiye", criticality, performance, openFindings, "GMP ve tedarik sürekliliği", "İzmir Tesis", Guid.NewGuid(), "Ayşe Denetçi", "Kalite Güvence", Guid.NewGuid(), "Satınalma Müdürü", Guid.NewGuid(), "Kanıt Doğrulayıcı", Guid.NewGuid(), "Kalite Onaylayanı", Now.AddDays(10), Now.AddDays(12), "SA-2026.1", [("Kalite Sistemleri", "Değişiklikler müşteriye zamanında bildiriliyor mu?", "GMP Bölüm 5")], Now);
}
