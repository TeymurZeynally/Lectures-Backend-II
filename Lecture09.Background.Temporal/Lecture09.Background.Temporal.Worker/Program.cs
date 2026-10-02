using Lecture09.Background.Temporal.Worker.BackgroundServices.VideoProcessing.Activities;
using Lecture09.Background.Temporal.Worker.BackgroundServices.VideoProcessing.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Temporalio.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddTemporalClient(builder.Configuration["Temporal:Address"]!, "default");

builder.Services
    .AddHostedTemporalWorker("video-processing")
    .AddScopedActivities<VideoActivities>()
    .AddWorkflow<ProcessVideoWorkflow>();

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>().LogInformation("Worker started, pid {Pid}", Environment.ProcessId);

host.Run();
