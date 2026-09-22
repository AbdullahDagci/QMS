using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qms.Infrastructure.Persistence;

namespace Qms.IntegrationTests;

public sealed class DatabasePrivilegeHardeningTests
{
    [Fact]
    public async Task ApplyAsync_RejectsUnsafeRuntimeRoleBeforeOpeningConnection()
    {
        var options = new DbContextOptionsBuilder<QmsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=not_used;Username=not_used;Password=not_used")
            .Options;
        await using var db = new QmsDbContext(options);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Database:RuntimeUsername"] = "qms_app; DROP ROLE qms_owner"
            }).Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DatabasePrivilegeHardening(db, configuration).ApplyAsync());
    }
}
