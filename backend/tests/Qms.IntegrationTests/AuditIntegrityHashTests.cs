using System.Text.Json;
using Qms.Domain.AuditTrail;
using Qms.Infrastructure.Integrity;

namespace Qms.IntegrationTests;

public sealed class AuditIntegrityHashTests
{
    [Fact]
    public void AuditHash_SurvivesPostgresJsonbAndTimestampRoundTrip()
    {
        var auditEvent = WrittenEvent();

        // PostgreSQL jsonb anahtarları yeniden sıralar ve boşluk ekler; timestamptz mikro saniyeye kırpar.
        SimulateDatabaseRoundTrip(auditEvent,
            """{"Code": "TEST", "Score": 1.50, "schemaHash": "dd24", "VersionNumber": 1}""");

        Assert.True(RecordIntegrityService.IsAuditHashValid(auditEvent, string.Empty));
    }

    [Fact]
    public void IsAuditHashValid_RejectsTamperedPayload()
    {
        var auditEvent = WrittenEvent();

        SimulateDatabaseRoundTrip(auditEvent,
            """{"Code": "HACK", "Score": 1.50, "schemaHash": "dd24", "VersionNumber": 1}""");

        Assert.False(RecordIntegrityService.IsAuditHashValid(auditEvent, string.Empty));
    }

    private static AuditEvent WrittenEvent()
    {
        var auditEvent = AuditEvent.Create("ElectronicFormDefinition", Guid.NewGuid(), 1, "ElectronicFormCreated",
            Guid.NewGuid(), "QMS İlk Yöneticisi",
            new DateTimeOffset(2026, 9, 4, 6, 40, 38, TimeSpan.Zero).AddTicks(6_678_061), "correlation",
            JsonSerializer.SerializeToDocument(new { Code = "TEST", VersionNumber = 1, schemaHash = "dd24", Score = 1.50m }));
        auditEvent.Seal(string.Empty, RecordIntegrityService.AuditHash(auditEvent, string.Empty), "mac");
        return auditEvent;
    }

    private static void SimulateDatabaseRoundTrip(AuditEvent auditEvent, string jsonbText)
    {
        var ticks = auditEvent.OccurredAtUtc.UtcTicks;
        typeof(AuditEvent).GetProperty(nameof(AuditEvent.Payload))!
            .SetValue(auditEvent, JsonDocument.Parse(jsonbText));
        typeof(AuditEvent).GetProperty(nameof(AuditEvent.OccurredAtUtc))!
            .SetValue(auditEvent, new DateTimeOffset(ticks - ticks % 10, TimeSpan.Zero));
    }
}
