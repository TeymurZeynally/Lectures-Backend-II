using Confluent.Kafka;
using Lecture04.Async.Kafka.Consumer.BackgroundServices;
using Lecture04.Async.Kafka.Consumer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection("Processing"));

builder.Services.AddHostedService<FeedingEventConsumer>();

builder.Build().Run();
