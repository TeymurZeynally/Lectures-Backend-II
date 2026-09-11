using System.Text.Json;
using Confluent.Kafka;
using Lecture04.Async.Kafka.Consumer.Options;
using Lecture04.Async.Kafka.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture04.Async.Kafka.Consumer.BackgroundServices;

public sealed class FeedingEventConsumer(
    IOptions<ConsumerConfig> kafkaOptions,
    IOptions<ConsumerOptions> options,
    ILogger<FeedingEventConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(() => Run(stoppingToken), TaskCreationOptions.LongRunning);
    }

    private void Run(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var clientId = kafkaOptions.Value.ClientId ?? "consumer";

        using var consumer = new ConsumerBuilder<string, string>(kafkaOptions.Value)
            .SetPartitionsAssignedHandler((_, partitions) => logger.LogWarning(
                "[{ClientId}] assigned partitions [{Partitions}]",
                clientId, string.Join(", ", partitions.Select(p => p.Partition.Value))))
            .SetPartitionsRevokedHandler((_, partitions) => logger.LogWarning(
                "[{ClientId}] revoked partitions [{Partitions}] (group is rebalancing)",
                clientId, string.Join(", ", partitions.Select(p => p.Partition.Value))))
            .Build();

        consumer.Subscribe(settings.Topic);

        logger.LogInformation(
            "Consumer {ClientId} joined group {GroupId}, subscribed to {Topic}",
            clientId, kafkaOptions.Value.GroupId, settings.Topic);

        var processedCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result is null)
            {
                continue;
            }

            var feedingEvent = JsonSerializer.Deserialize<FeedingEvent>(result.Message.Value)!;
            processedCount++;

            logger.LogInformation(
                "[{ClientId}] partition {Partition} offset {Offset} -> fed cat {CatId} (seq {Sequence}, {Processed} processed here)",
                clientId, result.Partition.Value, result.Offset.Value,
                feedingEvent.CatId, feedingEvent.SequenceNumber, processedCount);

            if (settings.ProcessingDelayMs > 0)
            {
                Thread.Sleep(settings.ProcessingDelayMs);
            }
        }
    }
}
