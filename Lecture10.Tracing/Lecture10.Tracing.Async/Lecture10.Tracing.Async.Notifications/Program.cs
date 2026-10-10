using Lecture10.Tracing.Async.Notifications.BackgroundServices.SmsNotifications.Workers;
using Lecture10.Tracing.Async.Notifications.External.SmsGateway;
using Lecture10.Tracing.Async.Notifications.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration), writeToProviders: true);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("notifications"))
    .WithLogging()
    .WithTracing(tracing => tracing.AddSource(AppTracing.Source.Name))
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased))
    .UseOtlpExporter();

builder.Services.AddSingleton<ISmsGatewayClient, SmsGatewayClient>();
builder.Services.AddHostedService<AppointmentCreatedConsumer>();

builder.Build().Run();
