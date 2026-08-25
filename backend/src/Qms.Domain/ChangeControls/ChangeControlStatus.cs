namespace Qms.Domain.ChangeControls;

public enum ChangeControlStatus
{
    Draft,
    PreliminaryReview,
    DepartmentReview,
    BoardReview,
    PlanApproval,
    Implementation,
    CommissioningApproval,
    PostImplementationVerification,
    ClosureApproval,
    Closed,
    RolledBack,
    Voided
}
