namespace Qms.Domain.Capas;

public enum CapaStatus
{
    Draft,
    ScopeApproval,
    RootCauseApproval,
    ActionPlanning,
    PlanApproval,
    Implementation,
    ActionVerification,
    EffectivenessWaiting,
    EffectivenessReview,
    ClosureApproval,
    Closed,
    Voided
}
