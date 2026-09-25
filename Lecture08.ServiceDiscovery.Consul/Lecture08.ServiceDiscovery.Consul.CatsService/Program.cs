using Lecture08.ServiceDiscovery.Consul.CatsService.BackgroundServices;
using Lecture08.ServiceDiscovery.Consul.CatsService.HealthChecks;
using Lecture08.ServiceDiscovery.Consul.CatsService.Options;
using Lecture08.ServiceDiscovery.Consul.CatsService.State;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var instance = builder.Configuration.GetSection("Instance").Get<InstanceOptions>()!;
builder.WebHost.UseUrls($"http://localhost:{instance.Port}");

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<InstanceOptions>(builder.Configuration.GetSection("Instance"));
builder.Services.Configure<ConsulOptions>(builder.Configuration.GetSection("Consul"));
builder.Services.AddSingleton<InstanceState>();
builder.Services.AddHealthChecks().AddCheck<InstanceHealthCheck>("instance");
builder.Services.AddHostedService<RequestsReporter>();
builder.Services.AddHostedService<ConsulRegistration>();

var app = builder.Build();

app.Use((context, next) =>
{
    context.Response.Headers["X-Instance"] = instance.Name;
    return next();
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
