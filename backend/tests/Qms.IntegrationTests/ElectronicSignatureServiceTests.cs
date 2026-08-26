using Qms.Application.Security;
using Qms.Infrastructure.ElectronicSignatures;

namespace Qms.IntegrationTests;

public sealed class ElectronicSignatureServiceTests
{
    [Fact]
    public void CreateInternal_ProducesDeterministicCanonicalSnapshotHash()
    {
        var currentUser = new TestCurrentUser();
        var service = new ElectronicSignatureService(null!, currentUser, null!);
        var qualityRecordId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var signedAt = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var firstContent = new Dictionary<string, object>
        {
            ["status"] = "Approved",
            ["score"] = 24,
        };
        var secondContent = new Dictionary<string, object>
        {
            ["score"] = 24,
            ["status"] = "Approved",
        };

        var first = service.CreateInternal(
            qualityRecordId,
            "Deviation",
            aggregateId,
            7,
            "approve",
            "Kalite değerlendirmesi onayı",
            firstContent,
            signedAt);
        var second = service.CreateInternal(
            qualityRecordId,
            "Deviation",
            aggregateId,
            7,
            "approve",
            "Kalite değerlendirmesi onayı",
            secondContent,
            signedAt);

        Assert.Equal(64, first.ContentHash.Length);
        Assert.Equal(first.ContentHash, second.ContentHash);
        Assert.Equal(
            first.SignedSnapshot.RootElement.GetRawText(),
            second.SignedSnapshot.RootElement.GetRawText());
        Assert.Equal("Kalite Onaylayanı", first.SignerDisplayNameSnapshot);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string DisplayName => "Kalite Onaylayanı";
        public Guid? DepartmentId => Guid.NewGuid();
        public IReadOnlySet<string> Roles { get; } = new HashSet<string>();
        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
