using Qms.Domain.SpecializedRecords;

namespace Qms.Domain.Tests.SpecializedRecords;

public sealed class SpecializedRecordTests
{
    [Fact]
    public void Artwork_requires_integrity_hash_before_review()
    {
        var now = DateTimeOffset.UtcNow;
        var record = Create(
            "M.13",
            "{\"proofDocumentNumber\":\"DOC-1\",\"proofVersion\":\"1.0\",\"proofSha256\":\"bad\"}",
            now
        );
        Assert.Throws<InvalidOperationException>(() => record.Submit(1, now));
    }

    [Fact]
    public void Oos_retest_cannot_be_performed_without_authorization()
    {
        var now = DateTimeOffset.UtcNow;
        var data =
            "{\"specification\":\"95-105%\",\"observedResult\":\"91%\",\"laboratoryInvestigation\":\"Cihaz ve numune incelendi\",\"retestPerformed\":true,\"retestAuthorized\":false}";
        Assert.Throws<InvalidOperationException>(() => Create("M.14", data, now).Submit(1, now));
    }

    [Fact]
    public void Supplier_score_and_three_way_control_can_close()
    {
        var now = DateTimeOffset.UtcNow;
        var record = Create(
            "M.16",
            "{\"evaluationPeriod\":\"2026-H1\",\"score\":88,\"qualityScore\":92,\"deliveryScore\":84,\"qualificationDecision\":\"Approved\"}",
            now
        );
        record.Submit(1, now);
        record.Review(2, now);
        record.Approve(3, now);
        record.Close(4, now);
        Assert.Equal(SpecializedRecordStatus.Closed, record.Status);
    }

    [Fact]
    public void Pv_serious_case_cannot_exceed_regulatory_deadline()
    {
        var now = DateTimeOffset.UtcNow;
        var receipt = now.ToString("O");
        var late = now.AddDays(16).ToString("O");
        var data =
            $"{{\"source\":\"Spontaneous\",\"patientCode\":\"P-1\",\"eventTerm\":\"Event\",\"seriousness\":\"Serious\",\"initialReceiptAtUtc\":\"{receipt}\",\"regulatoryDueAtUtc\":\"{late}\"}}";
        Assert.Throws<InvalidOperationException>(() => Create("M.15", data, now).Submit(1, now));
    }

    [Fact]
    public void Supplier_decision_must_match_total_score()
    {
        var now = DateTimeOffset.UtcNow;
        var data =
            "{\"evaluationPeriod\":\"2026-H1\",\"score\":55,\"qualityScore\":55,\"deliveryScore\":55,\"qualificationDecision\":\"Approved\"}";
        Assert.Throws<InvalidOperationException>(() => Create("M.16", data, now).Submit(1, now));
    }

    [Fact]
    public void Structured_payload_is_versioned_and_rejects_unbounded_or_non_object_json()
    {
        var now = DateTimeOffset.UtcNow;
        var record = Create("M.16",
            "{\"evaluationPeriod\":\"2026-H1\",\"score\":88,\"qualityScore\":92,\"deliveryScore\":84,\"qualificationDecision\":\"Approved\"}",
            now);

        Assert.Equal(1, record.SchemaVersion);
        Assert.Throws<ArgumentException>(() => Create("M.16", "[]", now));
        Assert.Throws<ArgumentException>(() => Create("M.16",
            $"{{\"value\":\"{new string('x', 33 * 1024)}\"}}", now));
    }

    private static SpecializedRecord Create(string module, string data, DateTimeOffset now) =>
        SpecializedRecord.Create(
            Guid.NewGuid(),
            module,
            "Başlık",
            "T",
            "Tür",
            "S",
            "Konu",
            "C",
            "Kapsam",
            "REF",
            "Açıklama",
            data,
            now.AddDays(5),
            Guid.NewGuid(),
            "Sorumlu",
            Guid.NewGuid(),
            "İnceleyen",
            Guid.NewGuid(),
            "Onaylayan",
            now
        );
}
