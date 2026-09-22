using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Qms.Infrastructure.Integrity;

namespace Qms.IntegrationTests;

public sealed class FinalReportIntegrityTests
{
    [Fact]
    public async Task SealAndVerify_RejectsSidecarTampering()
    {
        var root = Path.Combine(Path.GetTempPath(), "qms-report-integrity-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "report.pdf");
            var bytes = "%PDF-1.7 test"u8.ToArray();
            await File.WriteAllBytesAsync(path, bytes);
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["FileStorage:RootPath"] = root,
                    ["RecordIntegrity:HmacKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                }).Build();
            var service = new FinalReportIntegrity(configuration,
                new RecordIntegrityService(configuration));

            await service.SealAsync(path, hash);
            await service.VerifyAsync(path, hash);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.VerifyAsync(path, new string('0', 64)));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
