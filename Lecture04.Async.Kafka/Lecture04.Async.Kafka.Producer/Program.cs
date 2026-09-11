using Confluent.Kafka;
using Lecture04.Async.Kafka.Producer.BackgroundServices;
using Lecture04.Async.Kafka.Producer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection("Kafka"));

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
    new ProducerBuilder<string, string>(sp.GetRequiredService<IOptions<ProducerConfig>>().Value).Build());

builder.Services.AddHostedService<FeedingEventProducer>();

builder.Build().Run();
