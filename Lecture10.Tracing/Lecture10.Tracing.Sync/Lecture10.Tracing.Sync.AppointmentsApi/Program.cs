using Clinic.Grpc;
using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Repositories;
using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Services;
using Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;
using Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients;
using Lecture10.Tracing.Sync.AppointmentsApi.Tracing;
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
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation(http => http.EnrichWithHttpRequestMessage = (activity, request) => activity.DisplayName = $"{request.Method} {request.RequestUri!.AbsolutePath}"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased))
    .UseOtlpExporter();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAppointmentsService, AppointmentsService>();
builder.Services.AddScoped<IAppointmentsRepository, AppointmentsRepository>();
builder.Services.AddScoped<IScheduleClient, ScheduleClient>();
builder.Services.AddScoped<IPatientsClient, PatientsClient>();
builder.Services.AddGrpcClient<ScheduleService.ScheduleServiceClient>(
    o => o.Address = new Uri(builder.Configuration["ScheduleService:BaseUrl"]!));
builder.Services.AddGrpcClient<PatientsService.PatientsServiceClient>(
    o => o.Address = new Uri(builder.Configuration["PatientsService:BaseUrl"]!));
builder.Services.AddHttpClient<IPaymentsClient, PaymentsClient>(
    client => client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:BaseUrl"]!));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
