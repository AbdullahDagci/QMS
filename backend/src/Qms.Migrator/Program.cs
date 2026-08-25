using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qms.Infrastructure;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Identity;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddQmsInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Qms.Migrator");
var dbContext = scope.ServiceProvider.GetRequiredService<QmsDbContext>();

logger.LogInformation("Applying QMS database migrations");
await dbContext.Database.MigrateAsync();
await scope.ServiceProvider.GetRequiredService<QmsIdentitySeeder>().SeedAsync();
logger.LogInformation("QMS database migrations completed");
