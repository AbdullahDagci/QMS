using Qms.Contracts.Common;namespace Qms.Contracts.Trainings;
public sealed record TrainingSearchRequest(int Page=1,int PageSize=25,string SortBy="createdAtUtc",string SortDirection="desc",IReadOnlyList<ColumnFilterRequest>? Filters=null);
public sealed record CreateTrainingAssignmentRequest(Guid? MatrixRuleId,Guid? ControlledDocumentId,Guid? DocumentRevisionId,Guid EmployeeUserId,string Position,string CourseCode,string CourseTitle,string AssessmentMode,string DeliveryMethod,decimal PassingScore,int ValidityMonths,int MaxAttempts,bool IsCriticalQualification,DateTimeOffset DueAtUtc,string? SessionCode,string? Trainer,bool AssignNow=true);
public sealed record TransitionTrainingRequest(long ExpectedVersion,string Transition,string? Note=null);
public sealed record AcknowledgeTrainingRequest(long ExpectedVersion,string SignatureMeaning);
public sealed record RecordTrainingAssessmentRequest(long ExpectedVersion,decimal Score,bool PracticalPassed,string Evidence);
public sealed record CreateTrainingMatrixRuleRequest(string Position,string CourseCode,string CourseTitle,Guid? ControlledDocumentId,string AssessmentMode,string DeliveryMethod,decimal PassingScore,int ValidityMonths,bool IsCriticalQualification,DateTimeOffset EffectiveAtUtc);
