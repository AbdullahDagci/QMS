using Qms.Worker;
using Qms.Infrastructure;
using Qms.Application.Security;

var builder = Host.CreateApplicationBuilder(args);
Qms.Infrastructure.Configuration.SecretConfigurationLoader.Apply(builder.Configuration);
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration["RecordIntegrity:HmacKey"]))
        throw new InvalidOperationException("RecordIntegrity:HmacKey production ortamında zorunludur.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Smtp:Host"])
        || string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Smtp:Sender"]))
        throw new InvalidOperationException("SMTP host ve gönderici production ortamında zorunludur.");
    if (!string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Smtp:Username"])
        && string.IsNullOrWhiteSpace(builder.Configuration["Notifications:Smtp:Password"]))
        throw new InvalidOperationException("SMTP kullanıcı adı verildiğinde parola zorunludur.");
}
builder.Services.AddQmsInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUser, WorkerCurrentUser>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

file sealed class WorkerCurrentUser : ICurrentUser
{
    public Guid Id => Guid.Empty;
    public string DisplayName => "QMS Worker";
    public Guid? DepartmentId => null;
    public IReadOnlySet<string> Roles => new HashSet<string>(StringComparer.Ordinal);
    public bool IsInRole(string role) => false;
}
