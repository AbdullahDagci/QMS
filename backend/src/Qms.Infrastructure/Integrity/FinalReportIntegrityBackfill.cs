using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Qms.Infrastructure.Integrity;

public sealed class FinalReportIntegrityBackfill(IConfiguration configuration,
    FinalReportIntegrity reportIntegrity)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var configuredRoot = configuration["FileStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredRoot)) return;
        var root = Path.GetFullPath(configuredRoot);
        if (!Directory.Exists(root)) return;
        foreach (var path in Directory.EnumerateFiles(root, "*.pdf", SearchOption.AllDirectories))
        {
            var hashPath = path + ".sha256";
            if (!File.Exists(hashPath))
                throw new InvalidOperationException($"Nihai rapor SHA-256 yan dosyası eksik: {path}");
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var expected = (await File.ReadAllTextAsync(hashPath, cancellationToken)).Trim();
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Nihai rapor backfill bütünlük kontrolünü geçemedi: {path}");
            await reportIntegrity.SealAsync(path, actual, cancellationToken);
        }
    }
}
