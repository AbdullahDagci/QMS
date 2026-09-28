using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Domain.ElectronicSignatures;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Integrity;
using Qms.Infrastructure.Security;

namespace Qms.Infrastructure.ElectronicSignatures;

public sealed class ElectronicSignatureService(
    QmsDbContext db,
    ICurrentUser currentUser,
    UserManager<ApplicationUser> userManager,
    RecordIntegrityService? integrity = null) : IElectronicSignatureService
{
    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public async Task AuthenticateAsync(
        string? password,
        bool meaningAccepted,
        CancellationToken cancellationToken)
    {
        if (!meaningAccepted)
            throw new QmsForbiddenException("Elektronik imzanın anlamı ve sorumluluğu açıkça kabul edilmelidir.");
        if (string.IsNullOrWhiteSpace(password))
            throw new QmsForbiddenException("Elektronik imza için parola yeniden girilmelidir.");

        var account = await userManager.FindByIdAsync(currentUser.Id.ToString());
        if (account is null || !account.IsActive || !await userManager.CheckPasswordAsync(account, password))
            throw new QmsForbiddenException("Elektronik imza kimlik doğrulaması başarısız.");
    }

    public ElectronicSignature CreateInternal(
        Guid qualityRecordId,
        string aggregateType,
        Guid aggregateId,
        long recordVersion,
        string operation,
        string meaning,
        object signedContent,
        DateTimeOffset signedAtUtc,
        string? comment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(meaning);
        ArgumentNullException.ThrowIfNull(signedContent);

        var envelope = new
        {
            aggregateType = aggregateType.Trim(),
            aggregateId,
            qualityRecordId,
            recordVersion,
            operation = operation.Trim().ToLowerInvariant(),
            meaning = meaning.Trim(),
            content = signedContent
        };
        var snapshot = Canonicalize(JsonSerializer.SerializeToDocument(envelope, SnapshotOptions));
        var hash = Hash(snapshot);
        var macPayload = RecordIntegrityService.SignaturePayload(qualityRecordId, recordVersion, currentUser.Id,
            aggregateType, aggregateId, operation, meaning, signedAtUtc, hash);
        var integrityMac = integrity?.Mac(macPayload) ?? string.Empty;

        return ElectronicSignature.CreateInternal(
            qualityRecordId,
            recordVersion,
            currentUser.Id,
            currentUser.DisplayName,
            aggregateType,
            aggregateId,
            operation,
            meaning,
            signedAtUtc,
            snapshot,
            hash,
            integrityMac,
            comment);
    }

    public async Task<ElectronicSignatureVerification> VerifyAsync(
        Guid signatureId,
        CancellationToken cancellationToken)
    {
        var signature = await db.ElectronicSignatures.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == signatureId, cancellationToken)
            ?? throw new KeyNotFoundException("Elektronik imza bulunamadı.");
        if (!await db.VisibleQualityRecords(currentUser)
                .AnyAsync(item => item.Id == signature.QualityRecordId, cancellationToken))
            throw new KeyNotFoundException("Elektronik imza bulunamadı.");

        if (signature.ProviderType.Equals("Legacy", StringComparison.OrdinalIgnoreCase))
            return new ElectronicSignatureVerification(
                signature.Id,
                false,
                signature.ProviderType,
                signature.SignatureMethod,
                signature.AggregateType,
                signature.AggregateId,
                signature.RecordVersion,
                signature.Operation,
                signature.Meaning,
                signature.SignerDisplayNameSnapshot,
                signature.SignedAtUtc,
                signature.ContentHash,
                "Eski imza kaydı tam içerik snapshot'ı içermediği için yeni bütünlük doğrulamasına tabi değildir.");

        var actualHash = Hash(Canonicalize(signature.SignedSnapshot));
        var hashValid = FixedTimeEquals(actualHash, signature.ContentHash);
        var macPayload = RecordIntegrityService.SignaturePayload(signature);
        var macValid = integrity?.VerifyMac(macPayload, signature.IntegrityMac) == true;
        var valid = hashValid && macValid;
        return new ElectronicSignatureVerification(
            signature.Id,
            valid,
            signature.ProviderType,
            signature.SignatureMethod,
            signature.AggregateType,
            signature.AggregateId,
            signature.RecordVersion,
            signature.Operation,
            signature.Meaning,
            signature.SignerDisplayNameSnapshot,
            signature.SignedAtUtc,
            signature.ContentHash,
            valid
                ? "İmzalı kayıt snapshot'ı ve anahtarlı bütünlük mührü doğrulandı."
                : !hashValid
                    ? "İmzalı kayıt snapshot'ı bütünlük doğrulamasını geçemedi."
                    : "İmzanın anahtarlı bütünlük mührü doğrulanamadı.");
    }

    internal static JsonDocument Canonicalize(JsonDocument document) => CanonicalJson.Canonicalize(document);

    private static string Hash(JsonDocument snapshot) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot.RootElement.GetRawText())));

    private static bool FixedTimeEquals(string left, string right)
    {
        var a = Encoding.ASCII.GetBytes(left.ToLowerInvariant());
        var b = Encoding.ASCII.GetBytes(right.ToLowerInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

}
