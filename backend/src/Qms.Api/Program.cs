using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Threading.RateLimiting;
using Qms.Api.Endpoints;
using Qms.Api.Errors;
using Qms.Api.Health;
using Qms.Api.Security;
using Qms.Application.Security;
using Qms.Application.SystemCatalog;
using Qms.Contracts.System;
using Qms.Infrastructure;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
Qms.Infrastructure.Configuration.SecretConfigurationLoader.Apply(builder.Configuration);

if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("QmsDatabase")))
        throw new InvalidOperationException("Üretimde ConnectionStrings:QmsDatabase zorunludur.");
    var allowedHosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
        throw new InvalidOperationException("Üretimde AllowedHosts açıkça sınırlandırılmalıdır.");
    var integrityKey = builder.Configuration["RecordIntegrity:HmacKey"];
    if (string.IsNullOrWhiteSpace(integrityKey))
        throw new InvalidOperationException("Üretimde RecordIntegrity:HmacKey zorunludur.");
    if (builder.Configuration.GetValue("FileStorage:MalwareScanning:Required", true)
        && string.IsNullOrWhiteSpace(builder.Configuration["FileStorage:MalwareScanning:Host"]))
        throw new InvalidOperationException("Üretimde zararlı yazılım tarama servisi zorunludur.");
    var retentionYears = builder.Configuration.GetValue("FileStorage:MinimumRetentionYears", 10);
    if (retentionYears is < 1 or > 100)
        throw new InvalidOperationException("FileStorage:MinimumRetentionYears 1 ile 100 arasında olmalıdır.");
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<QmsExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddQmsInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<QmsDbContext>("postgresql")
    .AddCheck<MalwareScannerHealthCheck>("malware-scanner");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("anonymous-write", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
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
    .AddPolicy(QmsPolicies.FormView, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(QmsPolicies.FormUse, policy => policy.RequireRole(QmsRoles.All))
    .AddPolicy(
        QmsPolicies.FormManage,
        policy => policy.RequireRole(
            QmsRoles.Administrator,
            QmsRoles.QualityAssurance,
            QmsRoles.DocumentController
        )
    )
    .AddPolicy(
        QmsPolicies.FormApprove,
        policy => policy.RequireRole(QmsRoles.Administrator, QmsRoles.QualityAssurance)
    )
    .AddPolicy(
        QmsPolicies.AdministrationManage,
        policy => policy.RequireRole(QmsRoles.Administrator)
    );
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var value in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(value, out var address)) options.KnownProxies.Add(address);
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

app.Use(async (context, next) =>
{
    var correlationId = Qms.Infrastructure.Integrity.AuditCorrelation.Current;
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object>
           {
               ["CorrelationId"] = correlationId
           }))
        await next(context);
});

if (!app.Environment.IsDevelopment()) app.UseHsts();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        return Task.CompletedTask;
    });
    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("development");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var unsafeMethod = !HttpMethods.IsGet(context.Request.Method)
        && !HttpMethods.IsHead(context.Request.Method)
        && !HttpMethods.IsOptions(context.Request.Method);
    if (unsafeMethod
        && (context.Request.Cookies.ContainsKey(DevelopmentAuthenticationHandler.SessionCookieName)
            || context.Request.Cookies.ContainsKey(DevelopmentAuthenticationHandler.DevelopmentSessionCookieName))
        && context.Request.Headers["X-QMS-CSRF"] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "İstek doğrulanamadı",
            Detail = "Oturum çerezi kullanan değişiklik isteklerinde CSRF doğrulama başlığı zorunludur."
        });
        return;
    }
    await next(context);
});
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
app.MapElectronicFormEndpoints();
app.MapDashboardEndpoints();
app.MapSecurityEndpoints();
app.MapAccessAdministrationEndpoints();
app.MapNotificationEndpoints();
app.MapElectronicSignatureEndpoints();
app.MapManagedFileEndpoints();

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
