namespace Qms.Domain.Trainings;
public sealed class TrainingMatrixRule
{
    private TrainingMatrixRule() { }
    public Guid Id { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public string CourseCode { get; private set; } = string.Empty;
    public string CourseTitle { get; private set; } = string.Empty;
    public Guid? ControlledDocumentId { get; private set; }
    public TrainingAssessmentMode AssessmentMode { get; private set; }
    public TrainingDeliveryMethod DeliveryMethod { get; private set; }
    public decimal PassingScore { get; private set; }
    public int ValidityMonths { get; private set; }
    public bool IsCriticalQualification { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset EffectiveAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public static TrainingMatrixRule Create(string position, string code, string title, Guid? documentId, TrainingAssessmentMode mode, TrainingDeliveryMethod delivery, decimal score, int validityMonths, bool critical, DateTimeOffset effectiveAt, DateTimeOffset now)
    { Text(position, nameof(position), 160); Text(code, nameof(code), 80); Text(title, nameof(title), 240); if (score is < 0 or > 100) throw new ArgumentException("Geçme puanı 0–100 olmalıdır."); if (validityMonths is < 1 or > 120) throw new ArgumentException("Geçerlilik 1–120 ay olmalıdır."); return new() { Id=Guid.CreateVersion7(),Position=position.Trim(),CourseCode=code.Trim(),CourseTitle=title.Trim(),ControlledDocumentId=documentId,AssessmentMode=mode,DeliveryMethod=delivery,PassingScore=score,ValidityMonths=validityMonths,IsCriticalQualification=critical,IsActive=true,EffectiveAtUtc=effectiveAt,CreatedAtUtc=now }; }
    public void Deactivate() => IsActive = false;
    private static void Text(string v,string n,int max){if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max)throw new ArgumentException($"{n} zorunludur ve en fazla {max} karakter olabilir.");}
}

