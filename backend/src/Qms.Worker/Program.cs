using Qms.Worker;
using Qms.Infrastructure;
using Qms.Application.Security;

var builder = Host.CreateApplicationBuilder(args);
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
