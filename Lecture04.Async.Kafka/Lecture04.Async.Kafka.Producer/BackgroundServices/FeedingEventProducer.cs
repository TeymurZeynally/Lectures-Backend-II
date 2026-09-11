using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Lecture04.Async.Kafka.Contracts;
using Lecture04.Async.Kafka.Producer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture04.Async.Kafka.Producer.BackgroundServices;

public sealed class FeedingEventProducer(
    IProducer<string, string> producer,
    IOptions<ProducerOptions> options,
    ILogger<FeedingEventProducer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var random = new Random();
        long sequenceNumber = 0;

        logger.LogInformation(
            "Publishing to topic {Topic} at {Rate} msg/s for {CatCount} cats",
            settings.Topic, settings.MessagesPerSecond, settings.CatCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < settings.MessagesPerSecond; i++)
            {
                sequenceNumber++;
                var catId = random.Next(1, settings.CatCount + 1);
                var feedingEvent = new FeedingEvent(sequenceNumber, catId, DateTimeOffset.UtcNow);

                var message = new Message<string, string>
                {
                    Key = catId.ToString(),
                    Value = JsonSerializer.Serialize(feedingEvent)
                };

                producer.Produce(settings.Topic, message, report =>
                {
                    if (report.Error.IsError)
                    {
                        logger.LogError(
                            "Delivery failed for cat {CatId} seq {Sequence}: {Error}",
                            catId, feedingEvent.SequenceNumber, report.Error.Reason);
                    }
                });
            }

            producer.Poll(TimeSpan.Zero);
            logger.LogInformation("Published {Sequence} messages so far", sequenceNumber);

            var remaining = TimeSpan.FromSeconds(1) - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining);
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
    }
}
