using System.Diagnostics;
using System.Text.Json;
using Lecture04.Async.Nats.Contracts;
using Lecture04.Async.Nats.Producer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Lecture04.Async.Nats.Producer.BackgroundServices;

public sealed class JetStreamFeedingEventProducer(
    NatsConnection connection,
    IOptions<ProcessingOptions> options,
    ILogger<JetStreamFeedingEventProducer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var js = new NatsJSContext(connection);

        await js.CreateOrUpdateStreamAsync(new StreamConfig(settings.StreamName, [settings.Subject]), stoppingToken);

        var random = new Random();
        long sequenceNumber = 0;

        logger.LogInformation(
            "Publishing to stream {Stream} (subject {Subject}) at {Rate} msg/s for {CatCount} cats",
            settings.StreamName, settings.Subject, settings.MessagesPerSecond, settings.CatCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            var batchId = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var headers = new NatsHeaders { ["BatchId"] = batchId };

            logger.LogInformation("Publishing batch {BatchId} ({Count} messages)", batchId, settings.MessagesPerSecond);

            for (var i = 0; i < settings.MessagesPerSecond; i++)
            {
                sequenceNumber++;
                var catId = random.Next(1, settings.CatCount + 1);
                var feedingEvent = new FeedingEvent(sequenceNumber, catId, DateTimeOffset.UtcNow);
                var json = JsonSerializer.Serialize(feedingEvent);

                await js.PublishAsync(settings.Subject, json, headers: headers);
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
