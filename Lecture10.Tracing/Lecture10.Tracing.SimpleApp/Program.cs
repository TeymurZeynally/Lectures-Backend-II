using Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Services;
using Lecture10.Tracing.SimpleApp.Tracing;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration), writeToProviders: true);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("appointments-api"))
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddSource(AppTracing.Source.Name)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased))
    .UseOtlpExporter();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAppointmentsService, AppointmentsService>();
builder.Services.AddScoped<IAppointmentsRepository, AppointmentsRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
