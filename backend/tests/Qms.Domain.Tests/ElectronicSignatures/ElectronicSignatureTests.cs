using System.Text.Json;
using Qms.Domain.ElectronicSignatures;

namespace Qms.Domain.Tests.ElectronicSignatures;

public sealed class ElectronicSignatureTests
{
    [Fact]
    public void InternalSignature_BindsIdentityMeaningOperationAndSnapshot()
    {
        var qualityRecordId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var signerId = Guid.NewGuid();
        var signedAt = new DateTimeOffset(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
        using var snapshot = JsonDocument.Parse("""{"record":{"version":4,"status":"Approved"}}""");

        var signature = ElectronicSignature.CreateInternal(
            qualityRecordId,
            4,
            signerId,
            "Kalite Onaylayanı",
            "Deviation",
            aggregateId,
            "CLOSE",
            "Sapma nihai kapanış onayı",
            signedAt,
            snapshot,
            new string('a', 64),
            "Kanıtlar doğrulandı");

        Assert.Equal("Internal", signature.ProviderType);
        Assert.Equal("PasswordReauthentication", signature.SignatureMethod);
        Assert.Equal("Deviation", signature.AggregateType);
        Assert.Equal(aggregateId, signature.AggregateId);
        Assert.Equal("close", signature.Operation);
        Assert.Equal(4, signature.RecordVersion);
        Assert.Equal("Approved", signature.SignedSnapshot.RootElement.GetProperty("record").GetProperty("status").GetString());
        Assert.Equal(signerId, signature.SignerUserId);
        Assert.Equal(signedAt, signature.SignedAtUtc);
    }

    [Fact]
    public void InternalSignature_RequiresAnOperation()
    {
        using var snapshot = JsonDocument.Parse("{}");

        Assert.Throws<ArgumentException>(() => ElectronicSignature.CreateInternal(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            "Onaylayan",
            "Capa",
            Guid.NewGuid(),
            " ",
            "DÖF plan onayı",
            DateTimeOffset.UtcNow,
            snapshot,
            new string('b', 64)));
    }
}
