using Lecture04.Async.Nats.Producer.BackgroundServices;
using Lecture04.Async.Nats.Producer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<NatsOpts>(builder.Configuration.GetSection("Nats"));
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection("Processing"));

builder.Services.AddSingleton(sp => new NatsConnection(sp.GetRequiredService<IOptions<NatsOpts>>().Value));

var mode = builder.Configuration["Mode"] ?? "JetStream";

if (mode.Equals("Core", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<CoreFeedingEventProducer>();
}
else
{
    builder.Services.AddHostedService<JetStreamFeedingEventProducer>();
}

builder.Build().Run();
