using Qms.Worker;
using Qms.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddQmsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
