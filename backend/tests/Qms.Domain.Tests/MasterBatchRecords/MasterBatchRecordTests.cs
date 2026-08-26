using Qms.Domain.MasterBatchRecords;

namespace Qms.Domain.Tests.MasterBatchRecords;

public sealed class MasterBatchRecordTests
{
    [Fact]
    public void Controlled_record_requires_separated_actors_and_complete_critical_limits()
    {
        var now = DateTimeOffset.UtcNow;
        var author = Guid.NewGuid();
        var record = MasterBatchRecord.Create(
            Guid.NewGuid(),
            null,
            "P1",
            "Ürün",
            "TAB",
            "Tablet",
            "500 mg",
            100,
            "KG",
            "kg",
            "S1",
            "Tesis",
            "L1",
            "Hat",
            "1.0",
            "İlk sürüm",
            author,
            "Yazar",
            Guid.NewGuid(),
            "İnceleyen",
            Guid.NewGuid(),
            "Onaylayan",
            now
        );
        var step = MasterBatchStep.Create(
            record.Id,
            1,
            "GRN",
            "Granülasyon",
            "Sıcaklığı kontrol et",
            "SOP-1",
            true,
            "Sıcaklık",
            18,
            25,
            "C",
            "°C"
        );
        record.AddStep(1, step, now);
        record.SubmitReview(2, now);
        record.CompleteReview(3, now);
        record.Approve(4, now);
        record.MakeEffective(5, now.AddMinutes(1), now);
        Assert.Equal(MasterBatchRecordStatus.Effective, record.Status);
        Assert.Equal(6, record.Version);
    }

    [Fact]
    public void Same_person_cannot_hold_two_controlled_roles()
    {
        var user = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            MasterBatchRecord.Create(
                Guid.NewGuid(),
                null,
                "P1",
                "Ürün",
                "TAB",
                "Tablet",
                "500 mg",
                100,
                "KG",
                "kg",
                "S1",
                "Tesis",
                "L1",
                "Hat",
                "1.0",
                "İlk sürüm",
                user,
                "Yazar",
                user,
                "İnceleyen",
                Guid.NewGuid(),
                "Onaylayan",
                DateTimeOffset.UtcNow
            )
        );
    }
}
