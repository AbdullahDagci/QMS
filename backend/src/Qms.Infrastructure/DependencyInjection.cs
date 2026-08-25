using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qms.Application.Deviations;
using Qms.Application.Capas;
using Qms.Application.Dashboard;
using Qms.Infrastructure.Dashboard;
using Qms.Infrastructure.Deviations;
using Qms.Infrastructure.Capas;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;
using Qms.Application.Administration;
using Qms.Infrastructure.Administration;
using Qms.Application.ChangeControls;
using Qms.Infrastructure.ChangeControls;
using Qms.Application.Documents;
using Qms.Infrastructure.Documents;
using Qms.Application.Trainings;
using Qms.Infrastructure.Trainings;
using Qms.Application.Complaints;
using Qms.Infrastructure.Complaints;
using Qms.Application.InternalAudits;
using Qms.Infrastructure.InternalAudits;
using Qms.Application.ExternalAudits;
using Qms.Infrastructure.ExternalAudits;
using Qms.Application.SupplierAudits;
using Qms.Infrastructure.SupplierAudits;

namespace Qms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QmsDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:QmsDatabase yapılandırılmalıdır.");

        services.AddDbContext<QmsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(QmsDbContext).Assembly.FullName)));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDeviationService, DeviationService>();
        services.AddScoped<ICapaService, CapaService>();
        services.AddScoped<IChangeControlService, ChangeControlService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IInternalAuditService, InternalAuditService>();
        services.AddScoped<IExternalAuditService, ExternalAuditService>();
        services.AddScoped<ISupplierAuditService, SupplierAuditService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<QmsIdentitySeeder>();
        services.AddScoped<IAccessAdministrationService, AccessAdministrationService>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<QmsDbContext>();

        return services;
    }
}
