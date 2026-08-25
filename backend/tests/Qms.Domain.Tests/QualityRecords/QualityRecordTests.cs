using Qms.Domain.QualityRecords;

namespace Qms.Domain.Tests.QualityRecords;

public sealed class QualityRecordTests
{
    [Fact]
    public void Close_WhenOpenDependenciesExist_RejectsTransition()
    {
        var now = DateTimeOffset.UtcNow;
        var record = QualityRecord.Create(
            "SP-2026-000001",
            "deviation",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        record.Submit(now.AddMinutes(1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            record.Close(now.AddMinutes(2), hasOpenDependencies: true));

        Assert.Equal("Açık bağımlılıkları bulunan kalite kaydı kapatılamaz.", exception.Message);
        Assert.Equal(QualityRecordStatus.UnderAssessment, record.Status);
    }

    [Fact]
    public void Close_WhenDependenciesAreComplete_ClosesAndIncrementsVersion()
    {
        var now = DateTimeOffset.UtcNow;
        var record = QualityRecord.Create(
            "SP-2026-000002",
            "deviation",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        record.Submit(now.AddMinutes(1));
        record.Close(now.AddMinutes(2), hasOpenDependencies: false);

        Assert.Equal(QualityRecordStatus.Closed, record.Status);
        Assert.Equal(3, record.Version);
        Assert.Equal(now.AddMinutes(2), record.ClosedAtUtc);
    }
}
