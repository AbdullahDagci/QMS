namespace Qms.Domain.Trainings;
public sealed class TrainingAssessmentAttempt
{
    private TrainingAssessmentAttempt() { }
    public Guid Id { get; private set; }
    public Guid TrainingAssignmentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public decimal Score { get; private set; }
    public bool PracticalPassed { get; private set; }
    public bool Passed { get; private set; }
    public string Evidence { get; private set; } = string.Empty;
    public string Evaluator { get; private set; } = string.Empty;
    public DateTimeOffset AssessedAtUtc { get; private set; }
    internal static TrainingAssessmentAttempt Create(Guid assignmentId,int number,decimal score,bool practicalPassed,bool passed,string evidence,string evaluator,DateTimeOffset now){if(score is < 0 or > 100)throw new ArgumentException("Değerlendirme puanı 0–100 olmalıdır.");Text(evidence,nameof(evidence),2000);Text(evaluator,nameof(evaluator),160);return new(){Id=Guid.CreateVersion7(),TrainingAssignmentId=assignmentId,AttemptNumber=number,Score=score,PracticalPassed=practicalPassed,Passed=passed,Evidence=evidence.Trim(),Evaluator=evaluator.Trim(),AssessedAtUtc=now};}
    private static void Text(string v,string n,int max){if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max)throw new ArgumentException($"{n} zorunludur ve en fazla {max} karakter olabilir.");}
}

