using Qms.Domain.Files;

namespace Qms.Domain.Tests.Files;

public sealed class ManagedFileTests
{
    [Fact]
    public void Create_PreservesImmutableEvidenceMetadata()
    {
        var uploadedAt = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero);
        var file = ManagedFile.Create(
            Guid.NewGuid(), "Deviation", Guid.NewGuid(), "InvestigationEvidence", "kanıt.pdf",
            "/data/qms-files/2026/09/file.bin", "application/pdf", 128, new string('A', 64),
            new string('B', 64), Guid.NewGuid(), "Kalite Uzmanı", uploadedAt, uploadedAt.AddYears(10));

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(128, file.Size);
        Assert.Equal(uploadedAt.AddYears(10), file.RetainUntilUtc);
        Assert.Equal(new string('A', 64), file.ContentHash);
        Assert.Equal(new string('B', 64), file.IntegrityMac);
    }

    [Fact]
    public void Create_RejectsEmptyContentAndInvalidRetention()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => ManagedFile.Create(
            Guid.NewGuid(), "Deviation", Guid.NewGuid(), "Evidence", "empty.pdf", "/tmp/empty",
            "application/pdf", 0, "hash", "mac", Guid.NewGuid(), "User", now, now.AddYears(1)));
        Assert.Throws<ArgumentException>(() => ManagedFile.Create(
            Guid.NewGuid(), "Deviation", Guid.NewGuid(), "Evidence", "file.pdf", "/tmp/file",
            "application/pdf", 1, "hash", "mac", Guid.NewGuid(), "User", now, now));
    }
}
