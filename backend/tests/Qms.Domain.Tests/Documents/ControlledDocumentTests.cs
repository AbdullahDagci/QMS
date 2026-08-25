using Qms.Domain.Documents;
namespace Qms.Domain.Tests.Documents;

public sealed class ControlledDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Approval_Waits_For_All_Reviews()
    {
        var d = Create(["Üretim"], []); d.Transition(1, "start-writing", null, false, Now); d.Transition(2, "submit-review", null, false, Now);
        var first = d.Reviews.First(); d.CompleteReview(3, first.Id, true, "Uygun", Now);
        Assert.Throws<InvalidOperationException>(() => d.Transition(4, "submit-approval", null, false, Now));
    }

    [Fact]
    public void Mandatory_Training_Blocks_Effective_Release()
    {
        var d = ToApproval(["Üretim Operatörü"]); d.Transition(d.Version, "approve", null, false, Now);
        Assert.Equal(ControlledDocumentStatus.TrainingWaiting, d.Status);
        Assert.Throws<InvalidOperationException>(() => d.Transition(d.Version, "release", null, false, Now));
        var training = Assert.Single(d.TrainingRequirements); d.CompleteTraining(d.Version, training.Id, "Sınav %100", Now); d.Transition(d.Version, "release", null, false, Now);
        Assert.Equal(ControlledDocumentStatus.Effective, d.Status);
    }

    [Fact]
    public void Reviewed_Revision_Is_Immutable_And_New_Revision_Gets_New_Number()
    {
        var d = Create([], []); d.Transition(1, "start-writing", null, false, Now); d.Transition(2, "submit-review", null, false, Now);
        Assert.Throws<InvalidOperationException>(() => d.UpdateDraft(3, "Değiştir", "Özet", Now));
        foreach (var review in d.Reviews) d.CompleteReview(d.Version, review.Id, true, "Uygun", Now); d.Transition(d.Version, "submit-approval", null, false, Now); d.Transition(d.Version, "approve", null, false, Now); d.Transition(d.Version, "release", null, false, Now);
        d.Transition(d.Version, "request-revision", null, false, Now); d.StartRevision(d.Version, false, "Minör iyileştirme", [], [], Now);
        Assert.Equal("0.2", d.CurrentRevision.VersionLabel); Assert.Equal(ControlledDocumentStatus.Writing, d.Status);
    }

    [Fact]
    public void Active_Controlled_Copy_Blocks_Archive()
    {
        var d = ToApproval([]); d.Transition(d.Version, "approve", null, false, Now); d.Transition(d.Version, "release", null, false, Now); d.IssueCopy(d.Version, "KK-001", "Üretim", "Hat kopyası", null, Now); d.Transition(d.Version, "withdraw", "Yeni SOP yayımlandı", false, Now);
        Assert.Throws<InvalidOperationException>(() => d.Transition(d.Version, "archive", null, false, Now));
    }

    private static ControlledDocument ToApproval(IReadOnlyList<string> trainings) { var d = Create([], trainings); d.Transition(1, "start-writing", null, false, Now); d.Transition(2, "submit-review", null, false, Now); foreach (var review in d.Reviews) d.CompleteReview(d.Version, review.Id, true, "Uygun", Now); d.Transition(d.Version, "submit-approval", null, false, Now); return d; }
    private static ControlledDocument Create(IReadOnlyList<string> reviews, IReadOnlyList<string> trainings) => ControlledDocument.Create(Guid.NewGuid(), Guid.NewGuid(), "SOP-URT-001", "Dolum Hattı Temizlik SOP", "SOP", "Doküman Kontrol", "Üretim", "Kurum İçi", 12, Now.AddMinutes(-1), "Amaç, kapsam, sorumluluk ve uygulama adımları.", "İlk yayın", reviews, trainings, Now.AddHours(-1));
}
