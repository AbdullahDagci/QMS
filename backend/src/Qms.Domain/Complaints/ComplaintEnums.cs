namespace Qms.Domain.Complaints;

public enum ComplaintStatus { Received, Triage, PreliminaryResponse, Investigation, ImpactAssessment, CapaDecision, FinalResponseApproval, Closed, Cancelled }
public enum ComplaintSeverity { Minor, Major, Critical }
public enum ComplaintInvestigationStatus { Pending, Completed }
public enum ComplaintResponseType { Preliminary, Final }
public enum ComplaintResponseStatus { Draft, Approved }

