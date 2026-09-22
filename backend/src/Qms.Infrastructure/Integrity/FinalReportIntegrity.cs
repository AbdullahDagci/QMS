using System.Text;
using Microsoft.Extensions.Configuration;

namespace Qms.Infrastructure.Integrity;

public sealed class FinalReportIntegrity(IConfiguration configuration, RecordIntegrityService integrity)
{
    public static async Task SealNewOrVerifyAsync(IConfiguration configuration, string path,
        string sha256, bool isNew, CancellationToken cancellationToken = default)
    {
        var helper = new FinalReportIntegrity(configuration, new RecordIntegrityService(configuration));
        if (isNew) await helper.SealAsync(path, sha256, cancellationToken);
        else await helper.VerifyAsync(path, sha256, cancellationToken);
    }

    public async Task SealAsync(string path, string sha256, CancellationToken cancellationToken = default)
    {
        if (!integrity.IsConfigured) return;
        var sidecar = path + ".hmac";
        var mac = integrity.Mac(Payload(path, sha256));
        try
        {
            await using var stream = new FileStream(sidecar, FileMode.CreateNew, FileAccess.Write,
                FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
            var bytes = Encoding.ASCII.GetBytes(mac);
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        catch (IOException) when (File.Exists(sidecar))
        {
            // Aynı deterministik raporu eşzamanlı mühürleyen diğer süreç kazanmış olabilir.
        }
        await VerifyAsync(path, sha256, cancellationToken);
        MakeReadOnly(path);
        if (File.Exists(path + ".sha256")) MakeReadOnly(path + ".sha256");
        MakeReadOnly(sidecar);
    }

    public async Task VerifyAsync(string path, string sha256, CancellationToken cancellationToken = default)
    {
        if (!integrity.IsConfigured) return;
        var sidecar = path + ".hmac";
        if (!File.Exists(sidecar))
            throw new InvalidOperationException("Nihai raporun anahtarlı bütünlük mührü bulunamadı.");
        var expected = (await File.ReadAllTextAsync(sidecar, cancellationToken)).Trim();
        if (!integrity.VerifyMac(Payload(path, sha256), expected))
            throw new InvalidOperationException("Nihai raporun anahtarlı bütünlük mührü doğrulanamadı.");
    }

    private string Payload(string path, string sha256)
    {
        var root = Path.GetFullPath(configuration["FileStorage:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "qms-files"));
        var relative = Path.GetRelativePath(root, Path.GetFullPath(path)).Replace('\\', '/');
        if (relative.StartsWith("../", StringComparison.Ordinal) || relative == "..")
            throw new InvalidOperationException("Nihai rapor depolama kökünün dışında olamaz.");
        return $"final-report|{relative}|{sha256.ToLowerInvariant()}";
    }

    private static void MakeReadOnly(string path)
    {
        if (OperatingSystem.IsWindows())
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
        else
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.GroupRead);
    }
}
