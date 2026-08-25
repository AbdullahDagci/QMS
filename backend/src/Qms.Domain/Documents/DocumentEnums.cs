namespace Qms.Domain.Documents;

public enum ControlledDocumentStatus { Draft, Writing, Review, Approval, Approved, TrainingWaiting, Effective, RevisionPending, PeriodicReview, Withdrawn, Archived, Voided }
public enum DocumentRevisionStatus { Draft, InReview, Approved, Effective, Superseded, Withdrawn }
public enum DocumentReviewStatus { Pending, Approved, ChangesRequested }
public enum DocumentTrainingStatus { Pending, Completed }
public enum ControlledCopyStatus { Issued, Returned, Destroyed }
