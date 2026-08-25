namespace Qms.Domain.Deviations;

public enum DeviationStatus
{
    Draft = 0,
    Submitted = 1,
    PreliminaryReview = 2,
    Investigation = 3,
    ImpactAssessment = 4,
    QualityAssessment = 5,
    ActionImplementation = 6,
    EffectivenessReview = 7,
    ClosureApproval = 8,
    Closed = 9,
    Voided = 10
}
