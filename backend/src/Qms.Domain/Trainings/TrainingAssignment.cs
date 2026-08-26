namespace Qms.Domain.Trainings;
public sealed class TrainingAssignment
{
    private readonly List<TrainingAssessmentAttempt> _attempts=[];
    private TrainingAssignment() { }
    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public Guid? MatrixRuleId { get; private set; }
    public Guid? ControlledDocumentId { get; private set; }
    public Guid? DocumentRevisionId { get; private set; }
    public Guid? DocumentTrainingRequirementId { get; private set; }
    public Guid EmployeeUserId { get; private set; }
    public string EmployeeName { get; private set; }=string.Empty;
    public Guid PositionId { get; private set; }
    public string Position { get; private set; }=string.Empty;
    public string CourseCode { get; private set; }=string.Empty;
    public string CourseTitle { get; private set; }=string.Empty;
    public TrainingAssessmentMode AssessmentMode { get; private set; }
    public TrainingDeliveryMethod DeliveryMethod { get; private set; }
    public decimal PassingScore { get; private set; }
    public int ValidityMonths { get; private set; }
    public int MaxAttempts { get; private set; }
    public bool IsCriticalQualification { get; private set; }
    public int PlannedYear { get; private set; }
    public string? SessionCode { get; private set; }
    public string? Trainer { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; }
    public TrainingAssignmentStatus Status { get; private set; }
    public int ProgressPercent { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }
    public string? AcknowledgementMeaning { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public string? TrainerApprovalNote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<TrainingAssessmentAttempt> Attempts=>_attempts;
    public static TrainingAssignment Create(Guid qualityRecordId,Guid? matrixRuleId,Guid? documentId,Guid? revisionId,Guid? requirementId,Guid employeeId,string employee,Guid positionId,string position,string code,string title,TrainingAssessmentMode mode,TrainingDeliveryMethod delivery,decimal passingScore,int validityMonths,int maxAttempts,bool critical,DateTimeOffset dueAt,string? session,string? trainer,bool assigned,DateTimeOffset now)
    { if(employeeId==Guid.Empty)throw new ArgumentException("Çalışan zorunludur.");if(positionId==Guid.Empty)throw new ArgumentException("Pozisyon zorunludur.");Text(employee,nameof(employee),200);Text(position,nameof(position),160);Text(code,nameof(code),80);Text(title,nameof(title),240);if(passingScore is <0 or >100)throw new ArgumentException("Geçme puanı 0–100 olmalıdır.");if(validityMonths is <1 or >120)throw new ArgumentException("Geçerlilik 1–120 ay olmalıdır.");if(maxAttempts is <1 or >10)throw new ArgumentException("Deneme sayısı 1–10 olmalıdır.");return new(){Id=Guid.CreateVersion7(),QualityRecordId=qualityRecordId,MatrixRuleId=matrixRuleId,ControlledDocumentId=documentId,DocumentRevisionId=revisionId,DocumentTrainingRequirementId=requirementId,EmployeeUserId=employeeId,EmployeeName=employee.Trim(),PositionId=positionId,Position=position.Trim(),CourseCode=code.Trim(),CourseTitle=title.Trim(),AssessmentMode=mode,DeliveryMethod=delivery,PassingScore=passingScore,ValidityMonths=validityMonths,MaxAttempts=maxAttempts,IsCriticalQualification=critical,PlannedYear=dueAt.Year,SessionCode=Clean(session,80),Trainer=Clean(trainer,160),DueAtUtc=dueAt,Status=assigned?TrainingAssignmentStatus.Assigned:TrainingAssignmentStatus.Planned,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1};}
    public void Transition(long version,string transition,string? note,DateTimeOffset now){EnsureVersion(version);switch(transition.Trim().ToLowerInvariant()){case"assign":Ensure(TrainingAssignmentStatus.Planned);Move(TrainingAssignmentStatus.Assigned,now);break;case"start":if(Status is not(TrainingAssignmentStatus.Assigned or TrainingAssignmentStatus.Failed))throw new InvalidOperationException("Eğitim başlatılmaya uygun değil.");StartedAtUtc??=now;ProgressPercent=Math.Max(ProgressPercent,10);Move(TrainingAssignmentStatus.InProgress,now);break;case"submit-assessment":Ensure(TrainingAssignmentStatus.InProgress);if(AssessmentMode==TrainingAssessmentMode.ReadAndAcknowledge&&!AcknowledgedAtUtc.HasValue)throw new InvalidOperationException("Okuma ve anlama imzası olmadan eğitim tamamlanamaz.");ProgressPercent=75;Move(TrainingAssignmentStatus.Assessment,now);break;case"approve":Ensure(TrainingAssignmentStatus.TrainerApproval);Text(note??"",nameof(note),2000);TrainerApprovalNote=note!.Trim();ProgressPercent=100;CompletedAtUtc=now;ExpiresAtUtc=now.AddMonths(ValidityMonths);Move(TrainingAssignmentStatus.Completed,now);break;case"reassign":if(Status is not(TrainingAssignmentStatus.Failed or TrainingAssignmentStatus.Expired))throw new InvalidOperationException("Yalnız başarısız veya süresi dolmuş eğitim yeniden atanabilir.");ProgressPercent=0;StartedAtUtc=null;AcknowledgedAtUtc=null;AcknowledgementMeaning=null;CompletedAtUtc=null;ExpiresAtUtc=null;Move(TrainingAssignmentStatus.Assigned,now);break;case"expire":Ensure(TrainingAssignmentStatus.Completed);if(ExpiresAtUtc>now)throw new InvalidOperationException("Eğitimin geçerlilik süresi henüz dolmadı.");Move(TrainingAssignmentStatus.Expired,now);break;case"cancel":if(Status is TrainingAssignmentStatus.Completed or TrainingAssignmentStatus.Cancelled)throw new InvalidOperationException("Tamamlanmış eğitim iptal edilemez.");Text(note??"",nameof(note),2000);TrainerApprovalNote=note!.Trim();Move(TrainingAssignmentStatus.Cancelled,now);break;default:throw new ArgumentException("Bilinmeyen eğitim geçişi.");}}
    public void Acknowledge(long version,string meaning,DateTimeOffset now){EnsureVersion(version);Ensure(TrainingAssignmentStatus.InProgress);Text(meaning,nameof(meaning),500);AcknowledgementMeaning=meaning.Trim();AcknowledgedAtUtc=now;ProgressPercent=Math.Max(ProgressPercent,60);Touch(now);}
    public TrainingAssessmentAttempt RecordAttempt(long version,decimal score,bool practicalPassed,string evidence,string evaluator,DateTimeOffset now){EnsureVersion(version);Ensure(TrainingAssignmentStatus.Assessment);var practicalRequired=AssessmentMode is TrainingAssessmentMode.Practical or TrainingAssessmentMode.ExamAndPractical;var examRequired=AssessmentMode is TrainingAssessmentMode.Exam or TrainingAssessmentMode.ExamAndPractical;var passed=(!examRequired||score>=PassingScore)&&(!practicalRequired||practicalPassed);var attempt=TrainingAssessmentAttempt.Create(Id,_attempts.Count+1,score,practicalPassed,passed,evidence,evaluator,now);_attempts.Add(attempt);if(passed){ProgressPercent=90;Move(TrainingAssignmentStatus.TrainerApproval,now);}else if(_attempts.Count>=MaxAttempts){Move(TrainingAssignmentStatus.Failed,now);}else{Move(TrainingAssignmentStatus.InProgress,now);}return attempt;}
    private void EnsureVersion(long v){if(Version!=v)throw new InvalidOperationException("Eğitim kaydı başka bir kullanıcı tarafından değiştirilmiş. Ekranı yenileyin.");}private void Ensure(TrainingAssignmentStatus s){if(Status!=s)throw new InvalidOperationException($"Geçersiz eğitim geçişi. Beklenen: {s}, mevcut: {Status}.");}private void Move(TrainingAssignmentStatus s,DateTimeOffset n){Status=s;Touch(n);}private void Touch(DateTimeOffset n){UpdatedAtUtc=n;Version++;}private static void Text(string v,string n,int max){if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max)throw new ArgumentException($"{n} zorunludur ve en fazla {max} karakter olabilir.");}private static string? Clean(string?v,int max)=>string.IsNullOrWhiteSpace(v)?null:v.Trim().Length<=max?v.Trim():throw new ArgumentException($"Metin en fazla {max} karakter olabilir.");
}
