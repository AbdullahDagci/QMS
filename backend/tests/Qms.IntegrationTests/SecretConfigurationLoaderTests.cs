using Microsoft.Extensions.Configuration;
using Npgsql;
using Qms.Infrastructure.Configuration;

namespace Qms.IntegrationTests;

public sealed class SecretConfigurationLoaderTests
{
    [Fact]
    public void Apply_LoadsSecretsWithoutPuttingThemInEnvironmentConfiguration()
    {
        var root = Path.Combine(Path.GetTempPath(), "qms-secret-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var databaseSecret = Path.Combine(root, "database");
            var integritySecret = Path.Combine(root, "integrity");
            var bootstrapSecret = Path.Combine(root, "bootstrap");
            File.WriteAllText(databaseSecret, "db-secret\n");
            File.WriteAllText(integritySecret, Convert.ToBase64String(new byte[32]) + "\n");
            File.WriteAllText(bootstrapSecret, "Strong.Bootstrap!2026\n");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Secrets:DatabasePasswordFile"] = databaseSecret,
                    ["Secrets:RecordIntegrityHmacKeyFile"] = integritySecret,
                    ["Secrets:BootstrapAdminPasswordFile"] = bootstrapSecret,
                    ["Database:Host"] = "postgres.internal",
                    ["Database:Name"] = "qms_test",
                    ["Database:Username"] = "qms_user"
                }).Build();

            SecretConfigurationLoader.Apply(configuration);

            var connection = new NpgsqlConnectionStringBuilder(
                configuration.GetConnectionString("QmsDatabase"));
            Assert.Equal("postgres.internal", connection.Host);
            Assert.Equal("qms_test", connection.Database);
            Assert.Equal("qms_user", connection.Username);
            Assert.Equal("db-secret", connection.Password);
            Assert.Equal(Convert.ToBase64String(new byte[32]),
                configuration["RecordIntegrity:HmacKey"]);
            Assert.Equal("Strong.Bootstrap!2026", configuration["BootstrapAdmin:Password"]);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Apply_FailsClosedForMissingSecretFile()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Secrets:DatabasePasswordFile"] = "/definitely/not/a/qms/secret"
            }).Build();

        Assert.Throws<InvalidOperationException>(() => SecretConfigurationLoader.Apply(configuration));
    }
}
