using Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.GrpcHandlers;
using Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.Repositories;
using Lecture10.Tracing.Sync.ScheduleService.Tracing;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration), writeToProviders: true);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("schedule-service"))
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddSource(AppTracing.Source.Name)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased))
    .UseOtlpExporter();

builder.Services.AddGrpc();
builder.Services.AddScoped<ISlotsRepository, SlotsRepository>();

var app = builder.Build();

app.MapGrpcService<ScheduleGrpcHandler>();

app.Run();
