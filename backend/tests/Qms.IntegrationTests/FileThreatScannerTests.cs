using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qms.Api.Health;
using Qms.Infrastructure.Files;

namespace Qms.IntegrationTests;

public sealed class FileThreatScannerTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder().Build();

    [Fact]
    public async Task ScanAsync_RejectsImageWhoseSignatureDoesNotMatchContentType()
    {
        var path = TemporaryPath("png");
        try
        {
            await File.WriteAllTextAsync(path, "not a png");

            await Assert.ThrowsAsync<ArgumentException>(() => new FileThreatScanner(Configuration)
                .ScanAsync(path, "evidence.png", "image/png", CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ScanAsync_RejectsExecutableContentInsideOfficePackage()
    {
        var path = TemporaryPath("docx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                archive.CreateEntry("[Content_Types].xml");
                archive.CreateEntry("word/document.xml");
                archive.CreateEntry("word/payload.js");
            }

            await Assert.ThrowsAsync<ArgumentException>(() => new FileThreatScanner(Configuration)
                .ScanAsync(path, "evidence.docx",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ScanAsync_ReportsCorruptedOfficeArchiveAsValidationFailure()
    {
        var path = TemporaryPath("xlsx");
        try
        {
            await File.WriteAllBytesAsync(path, [0x50, 0x4b, 0x03, 0x04, 0xff, 0xff]);

            await Assert.ThrowsAsync<ArgumentException>(() => new FileThreatScanner(Configuration)
                .ScanAsync(path, "evidence.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false, HealthStatus.Healthy)]
    [InlineData(true, HealthStatus.Unhealthy)]
    public async Task MalwareHealth_ReflectsWhetherMissingScannerIsRequired(bool required,
        HealthStatus expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["FileStorage:MalwareScanning:Required"] = required.ToString()
            }).Build();

        var result = await new MalwareScannerHealthCheck(configuration)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(expected, result.Status);
    }

    private static string TemporaryPath(string extension) => Path.Combine(
        Path.GetTempPath(), $"qms-file-scan-{Guid.NewGuid():N}.{extension}");
}
