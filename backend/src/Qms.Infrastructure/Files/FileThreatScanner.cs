using System.Buffers.Binary;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Qms.Infrastructure.Files;

public sealed class FileThreatScanner(IConfiguration configuration)
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = [".pdf"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = [".xlsx"],
            ["image/png"] = [".png"],
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["text/plain"] = [".txt", ".csv"]
        };

    public async Task ScanAsync(string path, string fileName, string contentType,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (!AllowedTypes.TryGetValue(contentType, out var extensions)
            || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Dosya türü desteklenmiyor veya uzantı ile içerik türü eşleşmiyor.");

        await using (var stream = File.OpenRead(path))
        {
            var header = new byte[8];
            var read = await stream.ReadAsync(header, cancellationToken);
            if (read >= 2 && ((header[0] == (byte)'M' && header[1] == (byte)'Z')
                || (header[0] == 0x7f && header[1] == (byte)'E')))
                throw new InvalidOperationException("Çalıştırılabilir içerik kanıt dosyası olarak kabul edilmez.");
            if (contentType == "application/pdf" && (read < 5 || Encoding.ASCII.GetString(header, 0, 5) != "%PDF-"))
                throw new ArgumentException("PDF dosya imzası geçersizdir.");
            if (contentType.StartsWith("application/vnd.openxmlformats", StringComparison.Ordinal)
                && (read < 4 || header[0] != 0x50 || header[1] != 0x4b))
                throw new ArgumentException("Office Open XML dosya imzası geçersizdir.");
            if (contentType == "image/png" && (read < 8
                || !header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })))
                throw new ArgumentException("PNG dosya imzası geçersizdir.");
            if (contentType == "image/jpeg" && (read < 3
                || header[0] != 0xff || header[1] != 0xd8 || header[2] != 0xff))
                throw new ArgumentException("JPEG dosya imzası geçersizdir.");
        }
        if (contentType.StartsWith("application/vnd.openxmlformats", StringComparison.Ordinal))
        {
            try
            {
                ValidateOfficeArchive(path, contentType);
            }
            catch (InvalidDataException exception)
            {
                throw new ArgumentException("Office Open XML paketi geçersizdir.", exception);
            }
        }

        var host = configuration["FileStorage:MalwareScanning:Host"];
        var required = configuration.GetValue("FileStorage:MalwareScanning:Required", false);
        if (string.IsNullOrWhiteSpace(host))
        {
            if (required) throw new InvalidOperationException("Zararlı yazılım tarama servisi yapılandırılmamıştır.");
            return;
        }

        var port = configuration.GetValue("FileStorage:MalwareScanning:Port", 3310);
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);
        await using var network = client.GetStream();
        await network.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);
        await using var content = File.OpenRead(path);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var count = await content.ReadAsync(buffer, cancellationToken);
            if (count == 0) break;
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, count);
            await network.WriteAsync(length, cancellationToken);
            await network.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        await network.WriteAsync(new byte[4], cancellationToken);
        var responseBuffer = new byte[1024];
        var responseLength = await network.ReadAsync(responseBuffer, cancellationToken);
        var response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength).TrimEnd('\0', '\r', '\n');
        if (!response.EndsWith("OK", StringComparison.Ordinal))
            throw new InvalidOperationException("Dosya zararlı içerik taramasını geçemedi.");
    }

    private static void ValidateOfficeArchive(string path, string contentType)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count > 5000)
            throw new ArgumentException("Office paketi güvenli dosya sayısı sınırını aşıyor.");
        if (!archive.Entries.Any(entry => entry.FullName == "[Content_Types].xml"))
            throw new ArgumentException("Office Open XML paket manifesti bulunamadı.");
        var requiredPart = contentType.EndsWith("wordprocessingml.document", StringComparison.Ordinal)
            ? "word/document.xml" : "xl/workbook.xml";
        if (!archive.Entries.Any(entry => entry.FullName.Equals(requiredPart, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Office Open XML paketi bildirilen doküman türüyle eşleşmiyor.");
        long expandedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var normalized = entry.FullName.Replace('\\', '/');
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Split('/').Contains(".."))
                throw new ArgumentException("Office paketinde geçersiz dosya yolu bulundu.");
            expandedBytes += entry.Length;
            if (expandedBytes > 100 * 1024 * 1024L
                || entry.CompressedLength > 0 && entry.Length / Math.Max(1, entry.CompressedLength) > 200)
                throw new ArgumentException("Office paketi güvenli açılım sınırını aşıyor.");
            var extension = Path.GetExtension(normalized);
            if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".js", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".vbs", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Office paketi çalıştırılabilir veya makro içeriği barındıramaz.");
        }
    }
}
