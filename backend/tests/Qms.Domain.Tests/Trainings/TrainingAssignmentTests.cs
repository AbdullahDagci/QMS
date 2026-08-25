using Qms.Domain.Trainings;

namespace Qms.Domain.Tests.Trainings;

public sealed class TrainingAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Read_Training_Requires_Acknowledgement_Before_Assessment()
    {
        var assignment = Create(TrainingAssessmentMode.ReadAndAcknowledge);
        assignment.Transition(1, "start", null, Now);

        Assert.Throws<InvalidOperationException>(() => assignment.Transition(2, "submit-assessment", null, Now));

        assignment.Acknowledge(2, "Dokümanı okudum, anladım ve uygulayacağım.", Now);
        assignment.Transition(3, "submit-assessment", null, Now);
        Assert.Equal(TrainingAssignmentStatus.Assessment, assignment.Status);
    }

    [Fact]
    public void Passing_Assessment_And_Trainer_Approval_Create_Qualification()
    {
        var assignment = Create(TrainingAssessmentMode.Exam);
        assignment.Transition(1, "start", null, Now);
        assignment.Transition(2, "submit-assessment", null, Now);
        var attempt = assignment.RecordAttempt(3, 92, true, "Sınav formu EGT-F-01", "Eğitmen", Now);
        assignment.Transition(4, "approve", "Yeterlilik gözlemlendi ve onaylandı.", Now);

        Assert.True(attempt.Passed);
        Assert.Equal(TrainingAssignmentStatus.Completed, assignment.Status);
        Assert.Equal(100, assignment.ProgressPercent);
        Assert.Equal(Now.AddMonths(12), assignment.ExpiresAtUtc);
    }

    [Fact]
    public void Exhausted_Attempts_Fail_And_Can_Be_Reassigned()
    {
        var assignment = Create(TrainingAssessmentMode.Exam, maxAttempts: 2);
        assignment.Transition(1, "start", null, Now);
        assignment.Transition(2, "submit-assessment", null, Now);
        assignment.RecordAttempt(3, 40, true, "İlk sınav", "Eğitmen", Now);
        assignment.Transition(4, "submit-assessment", null, Now);
        assignment.RecordAttempt(5, 60, true, "İkinci sınav", "Eğitmen", Now);

        Assert.Equal(TrainingAssignmentStatus.Failed, assignment.Status);
        assignment.Transition(6, "reassign", null, Now.AddDays(1));
        Assert.Equal(TrainingAssignmentStatus.Assigned, assignment.Status);
        Assert.Equal(0, assignment.ProgressPercent);
    }

    private static TrainingAssignment Create(TrainingAssessmentMode mode, int maxAttempts = 3) => TrainingAssignment.Create(
        Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Üretim Kullanıcısı", "Sapma Bildiren",
        "SOP-URT-014", "Dolum Hattı Temizlik SOP · Sürüm 0.1", mode, TrainingDeliveryMethod.Electronic, 80, 12, maxAttempts,
        true, Now.AddDays(7), null, null, true, Now);
}
