using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qms.Application.Administration;
using Qms.Application.Capas;
using Qms.Application.ChangeControls;
using Qms.Application.Complaints;
using Qms.Application.Dashboard;
using Qms.Application.Deviations;
using Qms.Application.Documents;
using Qms.Application.ElectronicSignatures;
using Qms.Application.ExternalAudits;
using Qms.Application.InternalAudits;
using Qms.Application.MasterBatchRecords;
using Qms.Application.RiskManagement;
using Qms.Application.SpecializedRecords;
using Qms.Application.SupplierAudits;
using Qms.Application.Trainings;
using Qms.Application.WorkItems;
using Qms.Infrastructure.Administration;
using Qms.Infrastructure.Capas;
using Qms.Infrastructure.ChangeControls;
using Qms.Infrastructure.Complaints;
using Qms.Infrastructure.Dashboard;
using Qms.Infrastructure.Deviations;
using Qms.Infrastructure.Documents;
using Qms.Infrastructure.ElectronicSignatures;
using Qms.Infrastructure.ExternalAudits;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.InternalAudits;
using Qms.Infrastructure.MasterBatchRecords;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.RiskManagement;
using Qms.Infrastructure.SpecializedRecords;
using Qms.Infrastructure.SupplierAudits;
using Qms.Infrastructure.Trainings;
using Qms.Infrastructure.WorkItems;

namespace Qms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("QmsDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:QmsDatabase yapılandırılmalıdır."
            );

        services.AddDbContext<QmsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(QmsDbContext).Assembly.FullName)
            )
        );

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IElectronicSignatureService, ElectronicSignatureService>();
        services.AddScoped<IDeviationService, DeviationService>();
        services.AddScoped<IDeviationFinalReportService, DeviationFinalReportService>();
        services.AddScoped<ICapaService, CapaService>();
        services.AddScoped<ICapaFinalReportService, CapaFinalReportService>();
        services.AddScoped<IChangeControlService, ChangeControlService>();
        services.AddScoped<IChangeControlFinalReportService, ChangeControlFinalReportService>();
        services.AddScoped<IDocumentFinalReportService, DocumentFinalReportService>();
        services.AddScoped<ITrainingFinalReportService, TrainingFinalReportService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IComplaintFinalReportService, ComplaintFinalReportService>();
        services.AddScoped<IInternalAuditService, InternalAuditService>();
        services.AddScoped<IInternalAuditLookupService, InternalAuditService>();
        services.AddScoped<IInternalAuditFinalReportService, InternalAuditFinalReportService>();
        services.AddScoped<IExternalAuditFinalReportService, ExternalAuditFinalReportService>();
        services.AddScoped<ISupplierAuditFinalReportService, SupplierAuditFinalReportService>();
        services.AddScoped<IExternalAuditService, ExternalAuditService>();
        services.AddScoped<ISupplierAuditService, SupplierAuditService>();
        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<IWorkItemFinalReportService, WorkItemFinalReportService>();
        services.AddScoped<IRiskManagementService, RiskManagementService>();
        services.AddScoped<IRiskFinalReportService, RiskFinalReportService>();
        services.AddScoped<IMbrService, MbrService>();
        services.AddScoped<IMbrFinalReportService, MbrFinalReportService>();
        services.AddScoped<ISpecializedRecordService, SpecializedRecordService>();
        services.AddScoped<ISpecializedFinalReportService, SpecializedFinalReportService>();
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
