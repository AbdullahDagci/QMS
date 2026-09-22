using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Qms.Domain.AuditTrail;
using Qms.Domain.ElectronicSignatures;

namespace Qms.Infrastructure.Integrity;

public sealed class RecordIntegrityService
{
    private readonly byte[]? key;

    public RecordIntegrityService(IConfiguration configuration)
    {
        var encoded = configuration["RecordIntegrity:HmacKey"];
        if (!string.IsNullOrWhiteSpace(encoded))
        {
            try { key = Convert.FromBase64String(encoded); }
            catch (FormatException exception) { throw new InvalidOperationException("RecordIntegrity:HmacKey geçerli Base64 olmalıdır.", exception); }
            if (key.Length < 32) throw new InvalidOperationException("RecordIntegrity:HmacKey en az 256 bit olmalıdır.");
        }
    }

    public bool IsConfigured => key is not null;

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public string Mac(string value)
    {
        if (key is null) return string.Empty;
        return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)));
    }

    public bool VerifyMac(string value, string expected)
    {
        if (key is null || string.IsNullOrWhiteSpace(expected)) return false;
        byte[] actualBytes;
        byte[] expectedBytes;
        try
        {
            actualBytes = Convert.FromBase64String(Mac(value));
            expectedBytes = Convert.FromBase64String(expected);
        }
        catch (FormatException) { return false; }
        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    public static string AuditHash(AuditEvent auditEvent, string previousHash) => Hash(string.Join('|',
        previousHash,
        auditEvent.Id.ToString("N"),
        auditEvent.AggregateType,
        auditEvent.AggregateId.ToString("N"),
        auditEvent.AggregateVersion,
        auditEvent.EventType,
        auditEvent.ActorUserId.ToString("N"),
        auditEvent.ActorDisplayNameSnapshot,
        auditEvent.OccurredAtUtc.ToUniversalTime().ToString("O"),
        auditEvent.CorrelationId,
        auditEvent.Reason ?? string.Empty,
        auditEvent.Payload.RootElement.GetRawText()));

    public static string SignaturePayload(Guid qualityRecordId, long recordVersion,
        Guid signerUserId, string aggregateType, Guid aggregateId, string operation,
        string meaning, DateTimeOffset signedAtUtc, string contentHash) => string.Join('|',
            qualityRecordId.ToString("N"), recordVersion, signerUserId.ToString("N"),
            aggregateType.Trim(), aggregateId.ToString("N"), operation.Trim().ToLowerInvariant(),
            meaning.Trim(), signedAtUtc.ToUniversalTime().ToString("O"), contentHash);

    public static string SignaturePayload(ElectronicSignature signature) => SignaturePayload(
        signature.QualityRecordId, signature.RecordVersion, signature.SignerUserId,
        signature.AggregateType, signature.AggregateId, signature.Operation, signature.Meaning,
        signature.SignedAtUtc, signature.ContentHash);
}
