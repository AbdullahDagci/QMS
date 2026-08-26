using Qms.Domain.Deviations;

namespace Qms.Domain.Tests.Deviations;

public sealed class DeviationTests
{
    [Theory]
    [InlineData(1, 2, 3, 6, DeviationClassification.Minor, false)]
    [InlineData(3, 3, 3, 27, DeviationClassification.Major, true)]
    [InlineData(5, 5, 3, 75, DeviationClassification.Critical, true)]
    public void CreateDraft_CalculatesRiskClassificationAndCapaRequirement(
        int likelihood,
        int severity,
        int detectability,
        int expectedScore,
        DeviationClassification expectedClassification,
        bool expectedCapaRequirement)
    {
        var now = DateTimeOffset.UtcNow;

        var deviation = CreateDeviation(now, likelihood, severity, detectability);

        Assert.Equal(expectedScore, deviation.RiskScore);
        Assert.Equal(expectedClassification, deviation.Classification);
        Assert.Equal(expectedCapaRequirement, deviation.CapaRequired);
        Assert.Equal(DeviationStatus.Draft, deviation.Status);
    }

    [Fact]
    public void Submit_WithCurrentVersion_MovesDraftToSubmitted()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 3, 3, 3);

        deviation.Submit(expectedVersion: 1, now.AddMinutes(1));

        Assert.Equal(DeviationStatus.Submitted, deviation.Status);
        Assert.Equal(2, deviation.Version);
    }

    [Fact]
    public void Submit_WithStaleVersion_RejectsTransition()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 3, 3, 3);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            deviation.Submit(expectedVersion: 8, now.AddMinutes(1)));

        Assert.Contains("başka bir kullanıcı", exception.Message, StringComparison.Ordinal);
        Assert.Equal(DeviationStatus.Draft, deviation.Status);
    }

    [Fact]
    public void CompletePreliminaryReviewAndStartInvestigation_SkipsRedundantIntermediateAction()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 2, 2, 2);
        deviation.Submit(1, now.AddMinutes(1));

        deviation.CompletePreliminaryReviewAndStartInvestigation(
            2,
            "Ön inceleme tamamlandı; kök neden araştırması gerekli.",
            now.AddMinutes(2));

        Assert.Equal(DeviationStatus.Investigation, deviation.Status);
        Assert.Equal("Ön inceleme tamamlandı; kök neden araştırması gerekli.", deviation.PreliminaryReviewNote);
        Assert.Equal(3, deviation.Version);
    }

    [Fact]
    public void CreateDraft_WhenOccurrenceIsAfterDetection_RejectsInput()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => Deviation.CreateDraft(
            Guid.NewGuid(),
            "Dolum sıcaklığı sapması",
            "Dolum sıcaklığı limit dışına çıktı.",
            "Sıcaklık 25°C altında olmalıydı.",
            "Hat durduruldu ve ürün karantinaya alındı.",
            "Proses",
            "Üretim",
            "Dolum",
            now,
            now.AddMinutes(-1),
            2,
            3,
            2,
            now));
    }

    [Fact]
    public void CompleteInvestigation_WithoutCompletedInvestigation_BlocksTransition()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 1, 2, 3);
        deviation.Submit(1, now.AddMinutes(1));
        deviation.StartPreliminaryReview(2, "İlk değerlendirme tamamlandı.", now.AddMinutes(2));
        deviation.StartInvestigation(3, now.AddMinutes(3));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            deviation.CompleteInvestigation(4, hasCompletedInvestigation: false, now.AddMinutes(4)));

        Assert.Contains("tamamlanmış araştırma", exception.Message, StringComparison.Ordinal);
        Assert.Equal(DeviationStatus.Investigation, deviation.Status);
    }

    [Fact]
    public void MinorDeviation_WithCompletedAssessments_CanReachClosure()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 1, 2, 3);
        deviation.Submit(1, now.AddMinutes(1));
        deviation.StartPreliminaryReview(2, "Minör sapma ön incelemesi.", now.AddMinutes(2));
        deviation.StartInvestigation(3, now.AddMinutes(3));
        deviation.CompleteInvestigation(4, hasCompletedInvestigation: true, now.AddMinutes(4));
        deviation.CompleteImpactAssessment(5, hasPendingBatchDecision: false, now.AddMinutes(5));
        deviation.CompleteQualityAssessment(
            6,
            "DÖF gerekmiyor; yerinde düzeltme yeterli.",
            effectivenessRequired: false,
            hasLinkedCapa: false,
            now.AddMinutes(6));
        deviation.Close(7, "Tüm kapanış kriterleri karşılandı.", false, now.AddMinutes(7));

        Assert.Equal(DeviationStatus.Closed, deviation.Status);
        Assert.Equal(8, deviation.Version);
        Assert.Equal(now.AddMinutes(7), deviation.ClosedAtUtc);
    }

    [Fact]
    public void MajorDeviation_WithoutLinkedCapa_BlocksQualityAssessmentCompletion()
    {
        var now = DateTimeOffset.UtcNow;
        var deviation = CreateDeviation(now, 3, 3, 3);
        deviation.Submit(1, now.AddMinutes(1));
        deviation.StartPreliminaryReview(2, "Majör sapma ön incelemesi.", now.AddMinutes(2));
        deviation.StartInvestigation(3, now.AddMinutes(3));
        deviation.CompleteInvestigation(4, true, now.AddMinutes(4));
        deviation.CompleteImpactAssessment(5, false, now.AddMinutes(5));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            deviation.CompleteQualityAssessment(
                6,
                "DÖF açılmalıdır.",
                effectivenessRequired: true,
                hasLinkedCapa: false,
                now.AddMinutes(6)));

        Assert.Contains("ilişkili DÖF", exception.Message, StringComparison.Ordinal);
        Assert.Equal(DeviationStatus.QualityAssessment, deviation.Status);
    }

    [Fact]
    public void BatchImpact_WithPendingDecision_MustRemainLocked()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DeviationBatchImpact.Create(
            Guid.NewGuid(),
            "BATCH-001",
            isAffected: true,
            isLocked: false,
            BatchDisposition.Pending,
            "Laboratuvar sonucu bekleniyor.",
            Guid.NewGuid(),
            "Araştırmacı",
            "Kalite Güvence",
            DateTimeOffset.UtcNow));

        Assert.Contains("kilitli", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BatchImpact_ReleasedBatchCannotRemainLocked() =>
        Assert.Throws<InvalidOperationException>(() => DeviationBatchImpact.Create(Guid.NewGuid(), "BATCH-REL", true, true, BatchDisposition.Release, "Sonuçlar uygundur.", Guid.NewGuid(), "Değerlendirici", "Kalite Güvence", DateTimeOffset.UtcNow));

    [Fact]
    public void BatchImpact_UnaffectedBatchCannotBeRejected() =>
        Assert.Throws<InvalidOperationException>(() => DeviationBatchImpact.Create(Guid.NewGuid(), "BATCH-REJ", false, true, BatchDisposition.Reject, "Red kararı.", Guid.NewGuid(), "Değerlendirici", "Kalite Güvence", DateTimeOffset.UtcNow));

    [Fact]
    public void Investigation_PreservesActorAndDepartmentSnapshots()
    {
        var investigatorId = Guid.NewGuid();
        var investigation = DeviationInvestigation.CreateCompleted(Guid.NewGuid(), "5 Neden", "Proses", "Kök neden", "Sonuç", investigatorId, "Ayşe Araştırmacı", "Kalite Güvence", DateTimeOffset.UtcNow);

        Assert.Equal(investigatorId, investigation.InvestigatorUserId);
        Assert.Equal("Ayşe Araştırmacı", investigation.InvestigatorNameSnapshot);
        Assert.Equal("Kalite Güvence", investigation.InvestigatorDepartmentSnapshot);
    }

    [Fact]
    public void BatchImpact_PreservesAssessmentActorSnapshots()
    {
        var assessorId = Guid.NewGuid();
        var impact = DeviationBatchImpact.Create(Guid.NewGuid(), "BATCH-42", true, true, BatchDisposition.Hold, "İnceleme bekleniyor.", assessorId, "Mehmet Değerlendirici", "Üretim", DateTimeOffset.UtcNow);

        Assert.Equal(assessorId, impact.AssessedByUserId);
        Assert.Equal("Mehmet Değerlendirici", impact.AssessedByNameSnapshot);
        Assert.Equal("Üretim", impact.AssessedByDepartmentSnapshot);
    }

    private static Deviation CreateDeviation(
        DateTimeOffset now,
        int likelihood,
        int severity,
        int detectability) => Deviation.CreateDraft(
        Guid.NewGuid(),
        "Dolum sıcaklığı sapması",
        "Dolum sıcaklığı limit dışına çıktı.",
        "Sıcaklık 25°C altında olmalıydı.",
        "Hat durduruldu ve ürün karantinaya alındı.",
        "Proses",
        "Üretim",
        "Dolum",
        now.AddHours(-2),
        now.AddHours(-1),
        likelihood,
        severity,
        detectability,
        now);
}
