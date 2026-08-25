namespace Qms.Application.Security;

public static class QmsRoles
{
    public const string Administrator = "Administrator";
    public const string QualityAssurance = "QualityAssurance";
    public const string Approver = "Approver";
    public const string DeviationReporter = "DeviationReporter";
    public const string Investigator = "Investigator";
    public const string ActionOwner = "ActionOwner";
    public const string QualityViewer = "QualityViewer";
    public const string QualifiedPerson = "QualifiedPerson";
    public const string DepartmentManager = "DepartmentManager";
    public const string RegulatoryAffairs = "RegulatoryAffairs";
    public const string DocumentController = "DocumentController";
    public const string TrainingCoordinator = "TrainingCoordinator";
    public const string Learner = "Learner";
    public const string Trainer = "Trainer";

    public static readonly string[] All =
    [
        Administrator, QualityAssurance, Approver, DeviationReporter, Investigator, ActionOwner,
        QualityViewer, QualifiedPerson, DepartmentManager, RegulatoryAffairs, DocumentController,
        TrainingCoordinator, Learner, Trainer
    ];
}

public static class WorkflowTaskRoles
{
    public const string Initiator = "Initiator";
    public const string ProcessAuthority = "ProcessAuthority";
    public const string Investigator = "Investigator";
    public const string ActionOwner = "ActionOwner";
    public const string Evaluator = "Evaluator";
    public const string Approver = "Approver";
    public const string QualifiedPerson = "QualifiedPerson";
    public const string ChangeBoard = "ChangeBoard";
    public const string DocumentAuthor = "DocumentAuthor";
    public const string DocumentApprover = "DocumentApprover";
    public const string TrainingCoordinator = "TrainingCoordinator";
    public const string Learner = "Learner";
    public const string Trainer = "Trainer";
    public const string ComplaintCoordinator = "ComplaintCoordinator";
    public const string ComplaintInvestigator = "ComplaintInvestigator";
    public const string ResponseApprover = "ResponseApprover";
    public const string PharmacovigilanceReviewer = "PharmacovigilanceReviewer";
    public const string AuditPlanner = "AuditPlanner";
    public const string LeadAuditor = "LeadAuditor";
    public const string AuditeeResponder = "AuditeeResponder";
    public const string ExternalAuditCoordinator = "ExternalAuditCoordinator";
    public const string DocumentPackageController = "DocumentPackageController";
    public const string ExternalAuditeeResponder = "ExternalAuditeeResponder";
    public const string ExternalAuditAuthorizedCloser = "ExternalAuditAuthorizedCloser";
    public const string SupplierAuditPlanner = "SupplierAuditPlanner";
    public const string SupplierAuditLeadAuditor = "SupplierAuditLeadAuditor";
    public const string SupplierResponder = "SupplierResponder";
    public const string SupplierAuditVerifier = "SupplierAuditVerifier";
    public const string SupplierQualityApprover = "SupplierQualityApprover";
}

public static class QmsPolicies
{
    public const string QualityView = "quality.view";
    public const string DeviationCreate = "deviation.create";
    public const string DeviationInvestigate = "deviation.investigate";
    public const string DeviationManage = "deviation.manage";
    public const string CapaPlan = "capa.plan";
    public const string CapaCompleteAction = "capa.complete-action";
    public const string CapaVerify = "capa.verify";
    public const string CapaManage = "capa.manage";
    public const string ChangeCreate = "change.create";
    public const string ChangeReview = "change.review";
    public const string ChangeExecute = "change.execute";
    public const string ChangeApprove = "change.approve";
    public const string DocumentCreate = "document.create";
    public const string DocumentWrite = "document.write";
    public const string DocumentReview = "document.review";
    public const string DocumentApprove = "document.approve";
    public const string DocumentDistribute = "document.distribute";
    public const string DocumentRead = "document.read";
    public const string TrainingView = "training.view";
    public const string TrainingManage = "training.manage";
    public const string TrainingComplete = "training.complete";
    public const string TrainingApprove = "training.approve";
    public const string ComplaintView = "complaint.view";
    public const string ComplaintCreate = "complaint.create";
    public const string ComplaintInvestigate = "complaint.investigate";
    public const string ComplaintManage = "complaint.manage";
    public const string ComplaintApprove = "complaint.approve";
    public const string InternalAuditView = "internal-audit.view";
    public const string InternalAuditPlan = "internal-audit.plan";
    public const string InternalAuditExecute = "internal-audit.execute";
    public const string InternalAuditRespond = "internal-audit.respond";
    public const string InternalAuditApprove = "internal-audit.approve";
    public const string ExternalAuditView = "external-audit.view";
    public const string ExternalAuditCreate = "external-audit.create";
    public const string ExternalAuditPrepare = "external-audit.prepare";
    public const string ExternalAuditRespond = "external-audit.respond";
    public const string ExternalAuditApprove = "external-audit.approve";
    public const string SupplierAuditView = "supplier-audit.view";
    public const string SupplierAuditPlan = "supplier-audit.plan";
    public const string SupplierAuditExecute = "supplier-audit.execute";
    public const string SupplierAuditRespond = "supplier-audit.respond";
    public const string SupplierAuditApprove = "supplier-audit.approve";
    public const string AdministrationManage = "administration.manage";

    public static readonly string[] All =
    [
        QualityView, DeviationCreate, DeviationInvestigate, DeviationManage,
        CapaPlan, CapaCompleteAction, CapaVerify, CapaManage,
        ChangeCreate, ChangeReview, ChangeExecute, ChangeApprove,
        DocumentCreate, DocumentWrite, DocumentReview, DocumentApprove, DocumentDistribute, DocumentRead,
        TrainingView, TrainingManage, TrainingComplete, TrainingApprove,
        ComplaintView, ComplaintCreate, ComplaintInvestigate, ComplaintManage, ComplaintApprove,
        InternalAuditView, InternalAuditPlan, InternalAuditExecute, InternalAuditRespond, InternalAuditApprove,
        ExternalAuditView, ExternalAuditCreate, ExternalAuditPrepare, ExternalAuditRespond, ExternalAuditApprove,
        SupplierAuditView, SupplierAuditPlan, SupplierAuditExecute, SupplierAuditRespond, SupplierAuditApprove,
        AdministrationManage
    ];
}
