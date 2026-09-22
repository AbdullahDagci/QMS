using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qms.Application.Files;
using Qms.Application.Security;
using Qms.Domain.AuditTrail;
using Qms.Domain.Files;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Integrity;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Security;

namespace Qms.Infrastructure.Files;

public sealed class ManagedFileService(QmsDbContext db, ICurrentUser currentUser,
    TimeProvider timeProvider, IConfiguration configuration, RecordIntegrityService integrity,
    FileThreatScanner scanner) : IManagedFileService
{
    private static readonly IReadOnlyDictionary<string, string> AggregateTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["deviation"] = "Deviation", ["capa"] = "Capa", ["changecontrol"] = "ChangeControl",
            ["document"] = "Document", ["training"] = "Training", ["complaint"] = "Complaint",
            ["internalaudit"] = "InternalAudit", ["externalaudit"] = "ExternalAudit",
            ["supplieraudit"] = "SupplierAudit", ["workitem"] = "WorkItem",
            ["riskassessment"] = "RiskAssessment", ["masterbatchrecord"] = "MasterBatchRecord",
            ["specializedrecord"] = "SpecializedRecord", ["electronicformrecord"] = "ElectronicFormRecord"
        };

    public async Task<ManagedFileResponse> UploadAsync(string aggregateType, Guid aggregateId,
        string category, string fileName, string contentType, Stream content,
        DateTimeOffset? retainUntilUtc, CancellationToken cancellationToken)
    {
        var canonicalType = CanonicalType(aggregateType);
        if (!await CanModifyAggregateAsync(canonicalType, aggregateId, cancellationToken))
            throw new KeyNotFoundException("Kanıtın bağlanacağı kalite kaydı bulunamadı.");
        if (string.IsNullOrWhiteSpace(category) || category.Trim().Length > 128)
            throw new ArgumentException("Dosya kategorisi zorunludur ve en fazla 128 karakter olabilir.");
        var safeName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 255)
            throw new ArgumentException("Dosya adı geçersizdir.");

        var maxBytes = configuration.GetValue("FileStorage:MaxUploadBytes", 25 * 1024 * 1024L);
        var root = Path.GetFullPath(configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files"));
        var staging = Path.Combine(root, ".staging");
        Directory.CreateDirectory(staging);
        var id = Guid.CreateVersion7();
        var tempPath = Path.Combine(staging, id.ToString("N") + ".upload");
        string? finalPath = null;
        var persisted = false;
        long size = 0;
        string contentHash;
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                var buffer = new byte[64 * 1024];
                while (true)
                {
                    var read = await content.ReadAsync(buffer, cancellationToken);
                    if (read == 0) break;
                    size += read;
                    if (size > maxBytes) throw new ArgumentException($"Dosya en fazla {maxBytes / 1024 / 1024} MB olabilir.");
                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                await output.FlushAsync(cancellationToken);
            }
            if (size == 0) throw new ArgumentException("Boş dosya yüklenemez.");
            contentHash = Convert.ToHexStringLower(hash.GetHashAndReset());
            await scanner.ScanAsync(tempPath, safeName, contentType.Trim().ToLowerInvariant(), cancellationToken);

            var now = timeProvider.GetUtcNow();
            var minimumRetention = now.AddYears(configuration.GetValue("FileStorage:MinimumRetentionYears", 10));
            var requestedRetention = retainUntilUtc?.ToUniversalTime();
            var retention = requestedRetention.HasValue && requestedRetention.Value > minimumRetention
                ? requestedRetention.Value : minimumRetention;
            var relativePath = Path.Combine("managed", now.Year.ToString("0000"),
                now.Month.ToString("00"), id.ToString("N") + ".bin");
            finalPath = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!finalPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Dosya depolama yolu geçersizdir.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.Move(tempPath, finalPath);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(finalPath, UnixFileMode.UserRead | UnixFileMode.GroupRead);
            else File.SetAttributes(finalPath, FileAttributes.ReadOnly);

            var macPayload = MacPayload(id, canonicalType, aggregateId, category, safeName,
                contentType, size, contentHash, currentUser.Id, now, retention);
            var managedFile = ManagedFile.Create(id, canonicalType, aggregateId, category, safeName,
                relativePath, contentType, size, contentHash, integrity.Mac(macPayload), currentUser.Id,
                currentUser.DisplayName, now, retention);
            db.ManagedFiles.Add(managedFile);
            db.AuditEvents.Add(AuditEvent.Create(canonicalType, aggregateId, 0, "EvidenceFileUploaded",
                currentUser.Id, currentUser.DisplayName, now, Qms.Infrastructure.Integrity.AuditCorrelation.Current,
                System.Text.Json.JsonSerializer.SerializeToDocument(new
                {
                    fileId = id,
                    category = category.Trim(),
                    fileName = safeName,
                    contentType = contentType.Trim().ToLowerInvariant(),
                    size,
                    contentHash,
                    retention
                })));
            await db.SaveChangesAsync(cancellationToken);
            persisted = true;
            return Map(managedFile);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (!persisted && finalPath is not null && File.Exists(finalPath)) File.Delete(finalPath);
        }
    }

    public async Task<IReadOnlyList<ManagedFileResponse>> ListAsync(string aggregateType,
        Guid aggregateId, CancellationToken cancellationToken)
    {
        var canonicalType = CanonicalType(aggregateType);
        if (!await CanViewAggregateAsync(canonicalType, aggregateId, cancellationToken))
            throw new KeyNotFoundException("Kanıtın bağlandığı kalite kaydı bulunamadı.");
        var files = await db.ManagedFiles.AsNoTracking()
            .Where(file => file.AggregateType == canonicalType && file.AggregateId == aggregateId)
            .OrderByDescending(file => file.UploadedAtUtc)
            .ToListAsync(cancellationToken);
        return files.Select(Map).ToList();
    }

    public async Task<ManagedFileDownload?> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = await db.ManagedFiles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (file is null) return null;
        if (!await CanViewAggregateAsync(file.AggregateType, file.AggregateId, cancellationToken)) return null;
        var root = Path.GetFullPath(configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files"));
        var path = Path.GetFullPath(Path.Combine(root, file.StoragePath));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(path))
            throw new InvalidOperationException("Kanıt dosyası depoda bulunamadı.");
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var payload = MacPayload(file.Id, file.AggregateType, file.AggregateId, file.Category,
            file.OriginalFileName, file.ContentType, file.Size, file.ContentHash,
            file.UploadedByUserId, file.UploadedAtUtc, file.RetainUntilUtc);
        if (bytes.LongLength != file.Size || !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(hash), System.Text.Encoding.ASCII.GetBytes(file.ContentHash))
            || !integrity.VerifyMac(payload, file.IntegrityMac))
            throw new InvalidOperationException("Kanıt dosyasının bütünlük doğrulaması başarısız oldu.");
        return new ManagedFileDownload(bytes, file.OriginalFileName, file.ContentType, hash);
    }

    private static ManagedFileResponse Map(ManagedFile file) => new(file.Id, file.AggregateType,
        file.AggregateId, file.Category, file.OriginalFileName, file.ContentType, file.Size,
        file.ContentHash, file.UploadedByDisplayName, file.UploadedAtUtc, file.RetainUntilUtc);

    private static string CanonicalType(string value) =>
        AggregateTypes.TryGetValue(value.Replace("-", "").Replace("_", "").Trim(), out var canonical)
            ? canonical : throw new ArgumentException("Dosya bağlama kayıt türü desteklenmiyor.");

    private async Task<bool> CanViewAggregateAsync(string type, Guid id, CancellationToken ct)
    {
        var qualityRecordId = await db.ResolveQualityRecordIdAsync(type, id, ct);
        if (!qualityRecordId.HasValue
            || !await db.VisibleQualityRecords(currentUser).AnyAsync(record => record.Id == qualityRecordId.Value, ct))
            return false;
        if (type != "Document" || currentUser.IsInRole(QmsRoles.Administrator)
            || currentUser.IsInRole(QmsRoles.QualityAssurance)
            || currentUser.IsInRole(QmsRoles.DocumentController)) return true;
        return await db.ControlledDocuments.AsNoTracking().AnyAsync(document => document.Id == id
            && (document.Confidentiality != "Confidential" || document.OwnerUserId == currentUser.Id
                || db.RecordAccessGrants.Any(grant => grant.QualityRecordId == qualityRecordId.Value
                    && grant.UserId == currentUser.Id)), ct);
    }

    private async Task<bool> CanModifyAggregateAsync(string type, Guid id, CancellationToken ct)
    {
        var qualityRecordId = await db.ResolveQualityRecordIdAsync(type, id, ct);
        if (!qualityRecordId.HasValue) return false;
        var record = await db.VisibleQualityRecords(currentUser)
            .Where(item => item.Id == qualityRecordId.Value)
            .Select(item => new { item.CreatedByUserId, item.DepartmentId })
            .SingleOrDefaultAsync(ct);
        if (record is null || !await CanViewAggregateAsync(type, id, ct)) return false;
        if (currentUser.IsInRole(QmsRoles.Administrator)
            || currentUser.IsInRole(QmsRoles.QualityAssurance)
            || record.CreatedByUserId == currentUser.Id
            || currentUser.IsInRole(QmsRoles.DepartmentManager)
                && currentUser.DepartmentId == record.DepartmentId) return true;
        return await db.WorkflowTaskAssignments.AsNoTracking().AnyAsync(task =>
            task.AggregateType == type && task.AggregateId == id
            && task.AssignedUserId == currentUser.Id && task.Status == WorkflowTaskStatus.Active, ct);
    }

    private static string MacPayload(Guid id, string aggregateType, Guid aggregateId,
        string category, string fileName, string contentType, long size, string hash,
        Guid userId, DateTimeOffset uploadedAt, DateTimeOffset retainUntil) => string.Join('|',
            id.ToString("N"), aggregateType, aggregateId.ToString("N"), category.Trim(), fileName.Trim(),
            contentType.Trim().ToLowerInvariant(), size, hash, userId.ToString("N"),
            uploadedAt.ToUniversalTime().ToString("O"), retainUntil.ToUniversalTime().ToString("O"));
}
