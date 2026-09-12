using System.Text.Json;
using Lecture04.Async.Nats.Consumer.Options;
using Lecture04.Async.Nats.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace Lecture04.Async.Nats.Consumer.BackgroundServices;

public sealed class CoreFeedingEventConsumer(
    NatsConnection connection,
    IOptions<ProcessingOptions> options,
    ILogger<CoreFeedingEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        logger.LogInformation(
            "Consumer {InstanceName} joined queue group {QueueGroup} on subject {Subject} (core NATS, no persistence)",
            settings.InstanceName, settings.QueueGroup, settings.Subject);

        var processedCount = 0;

        await foreach (var msg in connection.SubscribeAsync<string>(settings.Subject, queueGroup: settings.QueueGroup, cancellationToken: stoppingToken))
        {
            var feedingEvent = JsonSerializer.Deserialize<FeedingEvent>(msg.Data!)!;
            processedCount++;

            var batchId = msg.Headers is not null && msg.Headers.TryGetValue("BatchId", out var batchIdValues)
                ? batchIdValues.ToString()
                : "?";

            logger.LogInformation(
                "[{InstanceName}] batch {BatchId} -> fed cat {CatId} (seq {Sequence}, {Processed} processed here)",
                settings.InstanceName, batchId, feedingEvent.CatId, feedingEvent.SequenceNumber, processedCount);

            if (settings.DelayMs > 0)
            {
                await Task.Delay(settings.DelayMs);
            }
        }
    }
}
