using System.Diagnostics;
using System.Text.Json;
using Lecture04.Async.RabbitMq.Contracts;
using Lecture04.Async.RabbitMq.Producer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Lecture04.Async.RabbitMq.Producer.BackgroundServices;

public sealed class FeedingEventProducer(
    IOptions<ConnectionFactory> connectionFactory,
    IOptions<ProcessingOptions> options,
    ILogger<FeedingEventProducer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        await using var connection = await connectionFactory.Value.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(settings.ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(settings.QueueName, settings.ExchangeName, settings.RoutingKey, cancellationToken: stoppingToken);

        var random = new Random();
        long sequenceNumber = 0;

        logger.LogInformation(
            "Publishing to exchange {Exchange} (routing key {RoutingKey}) at {Rate} msg/s for {CatCount} cats",
            settings.ExchangeName, settings.RoutingKey, settings.MessagesPerSecond, settings.CatCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var properties = new BasicProperties { Headers = new Dictionary<string, object?> { ["BatchId"] = batchId } };

            logger.LogInformation("Publishing batch {BatchId} ({Count} messages)", batchId, settings.MessagesPerSecond);

            for (var i = 0; i < settings.MessagesPerSecond; i++)
            {
                sequenceNumber++;
                var catId = random.Next(1, settings.CatCount + 1);
                var feedingEvent = new FeedingEvent(sequenceNumber, catId, DateTimeOffset.UtcNow);
                var body = JsonSerializer.SerializeToUtf8Bytes(feedingEvent);

                await channel.BasicPublishAsync(settings.ExchangeName, settings.RoutingKey, mandatory: false, properties, body);
            }

            logger.LogInformation("Published {Sequence} messages so far (batch {BatchId})", sequenceNumber, batchId);

            var remaining = TimeSpan.FromSeconds(1) - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining);
            }
        }
    }
}
