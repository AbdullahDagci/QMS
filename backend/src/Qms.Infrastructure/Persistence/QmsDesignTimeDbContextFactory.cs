using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Qms.Infrastructure.Persistence;

public sealed class QmsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<QmsDbContext>
{
    public QmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QmsDatabase")
            ?? "Host=localhost;Port=5432;Database=qms;Username=qms_app;Password=qms_dev_password";
        var options = new DbContextOptionsBuilder<QmsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(QmsDbContext).Assembly.FullName))
            .Options;
        return new QmsDbContext(options);
    }
}
