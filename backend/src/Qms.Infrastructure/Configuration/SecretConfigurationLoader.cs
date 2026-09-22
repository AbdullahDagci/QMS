using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Qms.Infrastructure.Configuration;

public static class SecretConfigurationLoader
{
    public static void Apply(IConfiguration configuration)
    {
        LoadFile(configuration, "Secrets:RecordIntegrityHmacKeyFile", "RecordIntegrity:HmacKey");
        LoadFile(configuration, "Secrets:SmtpPasswordFile", "Notifications:Smtp:Password");
        LoadFile(configuration, "Secrets:BootstrapAdminPasswordFile", "BootstrapAdmin:Password");

        var databasePassword = ReadFile(configuration["Secrets:DatabasePasswordFile"]);
        if (databasePassword is null || !string.IsNullOrWhiteSpace(
                configuration.GetConnectionString("QmsDatabase"))) return;
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration["Database:Host"] ?? "qms-postgres",
            Port = configuration.GetValue("Database:Port", 5432),
            Database = configuration["Database:Name"] ?? "qms",
            Username = configuration["Database:Username"] ?? "qms_app",
            Password = databasePassword,
            Pooling = true,
            IncludeErrorDetail = false
        };
        configuration["ConnectionStrings:QmsDatabase"] = builder.ConnectionString;
    }

    private static void LoadFile(IConfiguration configuration, string pathKey, string targetKey)
    {
        var value = ReadFile(configuration[pathKey]);
        if (value is not null) configuration[targetKey] = value;
    }

    private static string? ReadFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"Yapılandırılmış secret dosyası bulunamadı: {fullPath}");
        var value = File.ReadAllText(fullPath).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Secret dosyası boş olamaz: {fullPath}");
        return value;
    }
}
