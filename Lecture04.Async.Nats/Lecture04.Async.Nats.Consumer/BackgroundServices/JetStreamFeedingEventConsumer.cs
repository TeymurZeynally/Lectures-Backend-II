using System.Text.Json;
using Lecture04.Async.Nats.Consumer.Options;
using Lecture04.Async.Nats.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Lecture04.Async.Nats.Consumer.BackgroundServices;

public sealed class JetStreamFeedingEventConsumer(
    NatsConnection connection,
    IOptions<ProcessingOptions> options,
    ILogger<JetStreamFeedingEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var js = new NatsJSContext(connection);

        await js.CreateOrUpdateStreamAsync(new StreamConfig(settings.StreamName, [settings.Subject]), stoppingToken);

        var consumerConfig = new ConsumerConfig(settings.DurableConsumerName)
        {
            AckPolicy = ConsumerConfigAckPolicy.Explicit
        };
        var consumer = await js.CreateOrUpdateConsumerAsync(settings.StreamName, consumerConfig, stoppingToken);

        logger.LogInformation(
            "Consumer {InstanceName} bound to durable consumer {DurableConsumerName} on stream {Stream}",
            settings.InstanceName, settings.DurableConsumerName, settings.StreamName);

        var processedCount = 0;

        var consumeOpts = new NatsJSConsumeOpts { MaxMsgs = settings.PrefetchCount };

        await foreach (var msg in consumer.ConsumeAsync<string>(opts: consumeOpts, cancellationToken: stoppingToken))
        {
            var feedingEvent = JsonSerializer.Deserialize<FeedingEvent>(msg.Data!)!;
            processedCount++;

            var batchId = msg.Headers is not null && msg.Headers.TryGetValue("BatchId", out var batchIdValues)
                ? batchIdValues.ToString()
                : "?";

            logger.LogInformation(
                "[{InstanceName}] batch {BatchId} stream-seq {StreamSeq} -> fed cat {CatId} (seq {Sequence}, {Processed} processed here)",
                settings.InstanceName, batchId, msg.Metadata?.Sequence.Stream, feedingEvent.CatId, feedingEvent.SequenceNumber, processedCount);

            if (settings.DelayMs > 0)
            {
                await Task.Delay(settings.DelayMs);
            }

            await msg.AckAsync();
        }
    }
}
