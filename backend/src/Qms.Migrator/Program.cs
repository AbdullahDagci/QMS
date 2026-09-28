using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qms.Application.Security;
using Qms.Infrastructure;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Identity;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
Qms.Infrastructure.Configuration.SecretConfigurationLoader.Apply(builder.Configuration);
if (!builder.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration["RecordIntegrity:HmacKey"]))
    throw new InvalidOperationException("RecordIntegrity:HmacKey production ortamında zorunludur.");
builder.Services.AddQmsInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUser, MigratorCurrentUser>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Qms.Migrator");
var dbContext = scope.ServiceProvider.GetRequiredService<QmsDbContext>();

logger.LogInformation("Applying QMS database migrations");
await dbContext.Database.MigrateAsync();
await scope.ServiceProvider.GetRequiredService<Qms.Infrastructure.Integrity.QmsIntegrityBackfill>()
    .RunAsync();
await scope.ServiceProvider.GetRequiredService<Qms.Infrastructure.Integrity.FinalReportIntegrityBackfill>()
    .RunAsync();
await scope.ServiceProvider.GetRequiredService<Qms.Infrastructure.Security.RecordAccessBackfill>()
    .RunAsync();
await scope.ServiceProvider.GetRequiredService<QmsIdentitySeeder>()
    .SeedAsync(DevelopmentProfiles.AreEnabled(builder.Environment.IsDevelopment(), builder.Configuration));
await scope.ServiceProvider.GetRequiredService<DatabasePrivilegeHardening>()
    .ApplyAsync();
logger.LogInformation("QMS database migrations completed");

file sealed class MigratorCurrentUser : ICurrentUser
{
    public Guid Id => Guid.Empty;
    public string DisplayName => "QMS Migrator";
    public Guid? DepartmentId => null;
    public IReadOnlySet<string> Roles => new HashSet<string>(StringComparer.Ordinal);
    public bool IsInRole(string role) => false;
}
