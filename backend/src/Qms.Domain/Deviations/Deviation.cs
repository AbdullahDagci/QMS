namespace Qms.Domain.Deviations;

public sealed class Deviation
{
    private Deviation()
    {
    }

    public Guid Id { get; private set; }

    public Guid QualityRecordId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string ExpectedState { get; private set; } = string.Empty;

    public string ImmediateAction { get; private set; } = string.Empty;

    public string DeviationType { get; private set; } = string.Empty;

    public string DetectedDepartment { get; private set; } = string.Empty;

    public string ProcessStage { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset DetectedAtUtc { get; private set; }

    public DateTimeOffset TargetDateUtc { get; private set; }

    public int Likelihood { get; private set; }

    public int Severity { get; private set; }

    public int Detectability { get; private set; }

    public int RiskScore { get; private set; }

    public DeviationClassification Classification { get; private set; }

    public bool CapaRequired { get; private set; }

    public string? PreliminaryReviewNote { get; private set; }

    public string? QualityAssessmentNote { get; private set; }

    public bool EffectivenessRequired { get; private set; }

    public string? EffectivenessAssessmentNote { get; private set; }

    public string? ClosureJustification { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DeviationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Deviation CreateDraft(
        Guid qualityRecordId,
        string title,
        string description,
        string expectedState,
        string immediateAction,
        string deviationType,
        string detectedDepartment,
        string processStage,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset detectedAtUtc,
        int likelihood,
        int severity,
        int detectability,
        DateTimeOffset createdAtUtc)
    {
        if (qualityRecordId == Guid.Empty)
        {
            throw new ArgumentException("Kalite kaydı kimliği zorunludur.", nameof(qualityRecordId));
        }

        ValidateRequiredText(title, nameof(title), 200);
        ValidateRequiredText(description, nameof(description), 4000);
        ValidateRequiredText(expectedState, nameof(expectedState), 2000);
        ValidateRequiredText(immediateAction, nameof(immediateAction), 2000);
        ValidateRequiredText(deviationType, nameof(deviationType), 80);
        ValidateRequiredText(detectedDepartment, nameof(detectedDepartment), 160);
        ValidateRequiredText(processStage, nameof(processStage), 160);
        ValidateRiskFactor(likelihood, nameof(likelihood));
        ValidateRiskFactor(severity, nameof(severity));
        ValidateRiskFactor(detectability, nameof(detectability));

        if (occurredAtUtc > detectedAtUtc)
        {
            throw new ArgumentException("Gerçekleşme tarihi tespit tarihinden sonra olamaz.");
        }

        if (detectedAtUtc > createdAtUtc.AddMinutes(1))
        {
            throw new ArgumentException("Tespit tarihi gelecekte olamaz.");
        }

        var riskScore = likelihood * severity * detectability;
        var classification = CalculateClassification(riskScore);

        return new Deviation
        {
            Id = Guid.CreateVersion7(),
            QualityRecordId = qualityRecordId,
            Title = title.Trim(),
            Description = description.Trim(),
            ExpectedState = expectedState.Trim(),
            ImmediateAction = immediateAction.Trim(),
            DeviationType = deviationType.Trim(),
            DetectedDepartment = detectedDepartment.Trim(),
            ProcessStage = processStage.Trim(),
            OccurredAtUtc = occurredAtUtc,
            DetectedAtUtc = detectedAtUtc,
            TargetDateUtc = CalculateTargetDate(createdAtUtc, classification),
            Likelihood = likelihood,
            Severity = severity,
            Detectability = detectability,
            RiskScore = riskScore,
            Classification = classification,
            CapaRequired = classification is DeviationClassification.Major or DeviationClassification.Critical,
            Status = DeviationStatus.Draft,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            Version = 1
        };
    }

    public void Submit(long expectedVersion, DateTimeOffset occurredAtUtc)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("Sapma başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin.");
        }

        if (Status != DeviationStatus.Draft)
        {
            throw new InvalidOperationException("Yalnız taslak sapma iş akışına gönderilebilir.");
        }

        TransitionTo(DeviationStatus.Submitted, occurredAtUtc);
    }

    public void StartPreliminaryReview(
        long expectedVersion,
        string reviewNote,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.Submitted);
        ValidateRequiredText(reviewNote, nameof(reviewNote), 2000);

        PreliminaryReviewNote = reviewNote.Trim();
        TransitionTo(DeviationStatus.PreliminaryReview, occurredAtUtc);
    }

    public void StartInvestigation(long expectedVersion, DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.PreliminaryReview);
        TransitionTo(DeviationStatus.Investigation, occurredAtUtc);
    }

    public void CompleteInvestigation(
        long expectedVersion,
        bool hasCompletedInvestigation,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.Investigation);
        if (!hasCompletedInvestigation)
        {
            throw new InvalidOperationException("En az bir tamamlanmış araştırma olmadan etki değerlendirmesine geçilemez.");
        }

        TransitionTo(DeviationStatus.ImpactAssessment, occurredAtUtc);
    }

    public void RegisterInvestigation(long expectedVersion, DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.Investigation);
        UpdatedAtUtc = occurredAtUtc;
        Version++;
    }

    public void CompleteImpactAssessment(
        long expectedVersion,
        bool hasPendingBatchDecision,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.ImpactAssessment);
        if (hasPendingBatchDecision)
        {
            throw new InvalidOperationException("Kararı bekleyen batch/seri varken KG değerlendirmesine geçilemez.");
        }

        TransitionTo(DeviationStatus.QualityAssessment, occurredAtUtc);
    }

    public void RegisterBatchImpact(long expectedVersion, DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.ImpactAssessment);
        UpdatedAtUtc = occurredAtUtc;
        Version++;
    }

    public void CompleteQualityAssessment(
        long expectedVersion,
        string assessmentNote,
        bool effectivenessRequired,
        bool hasLinkedCapa,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.QualityAssessment);
        ValidateRequiredText(assessmentNote, nameof(assessmentNote), 4000);

        if (CapaRequired && !hasLinkedCapa)
        {
            throw new InvalidOperationException("Majör veya kritik sapma için ilişkili DÖF oluşturulmalıdır.");
        }

        QualityAssessmentNote = assessmentNote.Trim();
        EffectivenessRequired = effectivenessRequired;
        TransitionTo(
            hasLinkedCapa ? DeviationStatus.ActionImplementation :
            effectivenessRequired ? DeviationStatus.EffectivenessReview : DeviationStatus.ClosureApproval,
            occurredAtUtc);
    }

    public void CompleteEffectivenessReview(
        long expectedVersion,
        bool isEffective,
        string assessmentNote,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.EffectivenessReview);
        ValidateRequiredText(assessmentNote, nameof(assessmentNote), 4000);
        if (!isEffective)
        {
            throw new InvalidOperationException("Etkisiz sonuçla sapma kapanış onayına gönderilemez; ek aksiyon gerekir.");
        }

        EffectivenessAssessmentNote = assessmentNote.Trim();
        TransitionTo(DeviationStatus.ClosureApproval, occurredAtUtc);
    }

    public void CompleteLinkedCapa(
        long expectedVersion,
        bool linkedCapasClosed,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.ActionImplementation);
        if (!linkedCapasClosed)
        {
            throw new InvalidOperationException("İlişkili DÖF kapanmadan sapma aksiyon aşaması tamamlanamaz.");
        }

        TransitionTo(
            EffectivenessRequired ? DeviationStatus.EffectivenessReview : DeviationStatus.ClosureApproval,
            occurredAtUtc);
    }

    public void Close(
        long expectedVersion,
        string closureJustification,
        bool hasOpenDependencies,
        DateTimeOffset occurredAtUtc)
    {
        EnsureVersion(expectedVersion);
        EnsureStatus(DeviationStatus.ClosureApproval);
        ValidateRequiredText(closureJustification, nameof(closureJustification), 2000);
        if (hasOpenDependencies)
        {
            throw new InvalidOperationException("Açık bağımlılıkları bulunan sapma kapatılamaz.");
        }

        ClosureJustification = closureJustification.Trim();
        ClosedAtUtc = occurredAtUtc;
        TransitionTo(DeviationStatus.Closed, occurredAtUtc);
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("Sapma başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin.");
        }
    }

    private void EnsureStatus(DeviationStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Geçersiz sapma durum geçişi. Beklenen: {expected}, mevcut: {Status}.");
        }
    }

    private void TransitionTo(DeviationStatus nextStatus, DateTimeOffset occurredAtUtc)
    {
        Status = nextStatus;
        UpdatedAtUtc = occurredAtUtc;
        Version++;
    }

    private static DeviationClassification CalculateClassification(int riskScore) => riskScore switch
    {
        <= 20 => DeviationClassification.Minor,
        <= 50 => DeviationClassification.Major,
        _ => DeviationClassification.Critical
    };

    private static DateTimeOffset CalculateTargetDate(
        DateTimeOffset createdAtUtc,
        DeviationClassification classification) => classification switch
    {
        DeviationClassification.Critical => createdAtUtc.AddDays(2),
        DeviationClassification.Major => createdAtUtc.AddDays(7),
        _ => createdAtUtc.AddDays(15)
    };

    private static void ValidateRiskFactor(int value, string parameterName)
    {
        if (value is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Risk faktörü 1 ile 5 arasında olmalıdır.");
        }
    }

    private static void ValidateRequiredText(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        if (value.Trim().Length > maximumLength)
        {
            throw new ArgumentException($"Alan en fazla {maximumLength} karakter olabilir.", parameterName);
        }
    }
}
