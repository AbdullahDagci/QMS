using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Qms.Api.Endpoints;
using Qms.Api.Errors;
using Qms.Api.Security;
using Qms.Application.Security;
using Qms.Application.SystemCatalog;
using Qms.Contracts.System;
using Qms.Infrastructure;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<QmsExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddQmsInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddHealthChecks().AddDbContextCheck<QmsDbContext>("postgresql");
builder.Services.AddAuthorization();
builder
    .Services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName,
        _ => { }
    );
builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy(QmsPolicies.QualityView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.DeviationCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DeviationReporter
            )
    )
    .AddPolicy(
        QmsPolicies.DeviationInvestigate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Investigator
            )
    )
    .AddPolicy(
        QmsPolicies.DeviationManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.Investigator,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.CapaPlan,
        policy => policy.RequireRole(QmsRoles.Administrator, QmsRoles.QualityAssurance)
    )
    .AddPolicy(
        QmsPolicies.CapaCompleteAction,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.CapaVerify,
        policy => policy.RequireRole(QmsRoles.Administrator, QmsRoles.QualityAssurance)
    )
    .AddPolicy(
        QmsPolicies.CapaManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.ChangeCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.ChangeReview,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.RegulatoryAffairs
            )
    )
    .AddPolicy(
        QmsPolicies.ChangeExecute,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.ChangeApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.DepartmentManager,
                QmsRoles.RegulatoryAffairs
            )
    )
    .AddPolicy(
        QmsPolicies.DocumentCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController
            )
    )
    .AddPolicy(
        QmsPolicies.DocumentWrite,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController
            )
    )
    .AddPolicy(
        QmsPolicies.DocumentReview,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.RegulatoryAffairs
            )
    )
    .AddPolicy(
        QmsPolicies.DocumentApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.DocumentController,
                QmsRoles.TrainingCoordinator
            )
    )
    .AddPolicy(
        QmsPolicies.DocumentDistribute,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController
            )
    )
    .AddPolicy(QmsPolicies.DocumentRead, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(QmsPolicies.TrainingView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.TrainingManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.TrainingCoordinator
            )
    )
    .AddPolicy(QmsPolicies.TrainingComplete, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.TrainingApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.TrainingCoordinator,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(QmsPolicies.ComplaintView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.ComplaintCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DeviationReporter,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.ComplaintInvestigate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Investigator,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.ComplaintManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.ComplaintApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(QmsPolicies.InternalAuditView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.InternalAuditPlan,
        policy => policy.RequireRole(QmsRoles.Administrator, QmsRoles.QualityAssurance)
    )
    .AddPolicy(
        QmsPolicies.InternalAuditExecute,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Investigator,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.InternalAuditRespond,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.InternalAuditApprove,
        policy =>
            policy.RequireRole(QmsRoles.Administrator, QmsRoles.QualityAssurance, QmsRoles.Approver)
    )
    .AddPolicy(QmsPolicies.ExternalAuditView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.ExternalAuditCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.ExternalAuditPrepare,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController,
                QmsRoles.Investigator,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.ExternalAuditRespond,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.ExternalAuditApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(QmsPolicies.SupplierAuditView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.SupplierAuditPlan,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.SupplierAuditExecute,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Investigator,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.SupplierAuditRespond,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.SupplierAuditApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(QmsPolicies.WorkTrackingView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.WorkTrackingCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.ActionOwner
            )
    )
    .AddPolicy(
        QmsPolicies.WorkTrackingManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.ActionOwner,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.WorkTrackingVerify,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(QmsPolicies.RiskView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.RiskCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.Investigator
            )
    )
    .AddPolicy(
        QmsPolicies.RiskManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.Investigator,
                QmsRoles.ActionOwner,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.RiskApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(QmsPolicies.MbrView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.MbrCreate,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.MbrWrite,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DocumentController,
                QmsRoles.DepartmentManager,
                QmsRoles.Investigator,
                QmsRoles.Approver
            )
    )
    .AddPolicy(
        QmsPolicies.MbrReview,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager
            )
    )
    .AddPolicy(
        QmsPolicies.MbrApprove,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(QmsPolicies.SpecializedView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.SpecializedManage,
        policy =>
            policy.RequireRole(
                QmsRoles.Administrator,
                QmsRoles.QualityAssurance,
                QmsRoles.DepartmentManager,
                QmsRoles.Investigator,
                QmsRoles.Approver,
                QmsRoles.QualifiedPerson
            )
    )
    .AddPolicy(
        QmsPolicies.AdministrationManage,
        policy => policy.RequireRole(QmsRoles.Administrator)
    );
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "development",
        policy =>
            policy
                .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
    );
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("development");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
        "/api/v1/system/info",
        () =>
            Results.Ok(
                new SystemInfoResponse(
                    "QMS",
                    typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                    true,
                    QmsModuleCatalog.Modules
                )
            )
    )
    .WithName("GetSystemInfo")
    .WithTags("System");

app.MapDeviationEndpoints();
app.MapCapaEndpoints();
app.MapChangeControlEndpoints();
app.MapDocumentEndpoints();
app.MapTrainingEndpoints();
app.MapComplaintEndpoints();
app.MapInternalAuditEndpoints();
app.MapExternalAuditEndpoints();
app.MapSupplierAuditEndpoints();
app.MapWorkItemEndpoints();
app.MapRiskManagementEndpoints();
app.MapMbrEndpoints();
app.MapSpecializedRecordEndpoints();
app.MapDashboardEndpoints();
app.MapSecurityEndpoints();
app.MapAccessAdministrationEndpoints();
app.MapNotificationEndpoints();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
