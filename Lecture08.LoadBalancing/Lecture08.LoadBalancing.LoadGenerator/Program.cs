using Lecture08.LoadBalancing.LoadGenerator.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<LoadGeneratorOptions>(builder.Configuration.GetSection("LoadGenerator"));

builder.Services.AddSingleton(sp => new HttpClient(new SocketsHttpHandler
{
    MaxConnectionsPerServer = sp.GetRequiredService<IOptions<LoadGeneratorOptions>>().Value.MaxConnections
}));

builder.Services.AddHostedService<LoadGenerator>();

builder.Build().Run();
