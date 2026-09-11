using Lecture04.Async.RabbitMq.Producer.BackgroundServices;
using Lecture04.Async.RabbitMq.Producer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ConnectionFactory>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection("Processing"));

builder.Services.AddHostedService<FeedingEventProducer>();

builder.Build().Run();
