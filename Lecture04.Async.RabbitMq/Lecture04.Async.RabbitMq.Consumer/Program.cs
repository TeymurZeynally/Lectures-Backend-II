using Lecture04.Async.RabbitMq.Consumer.BackgroundServices;
using Lecture04.Async.RabbitMq.Consumer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ConnectionFactory>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection("Processing"));

builder.Services.AddHostedService<FeedingEventConsumer>();

builder.Build().Run();
