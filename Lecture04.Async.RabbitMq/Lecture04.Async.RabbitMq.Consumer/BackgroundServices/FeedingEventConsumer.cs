using System.Text;
using System.Text.Json;
using Lecture04.Async.RabbitMq.Consumer.Options;
using Lecture04.Async.RabbitMq.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lecture04.Async.RabbitMq.Consumer.BackgroundServices;

public sealed class FeedingEventConsumer(
    IOptions<ConnectionFactory> connectionFactory,
    IOptions<ProcessingOptions> options,
    ILogger<FeedingEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        await using var connection = await connectionFactory.Value.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(settings.ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(settings.QueueName, settings.ExchangeName, settings.RoutingKey, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, (ushort)settings.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        var processedCount = 0;

        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var feedingEvent = JsonSerializer.Deserialize<FeedingEvent>(delivery.Body.Span)!;
            processedCount++;

            var batchId = delivery.BasicProperties.Headers?.TryGetValue("BatchId", out var batchIdValue) == true
                ? batchIdValue switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    string s => s,
                    _ => "?"
                }
                : "?";

            logger.LogInformation(
                "[{ConsumerTag}] batch {BatchId} delivery {DeliveryTag} -> fed cat {CatId} (seq {Sequence}, {Processed} processed here)",
                settings.ConsumerTag, batchId, delivery.DeliveryTag, feedingEvent.CatId, feedingEvent.SequenceNumber, processedCount);

            if (settings.DelayMs > 0)
            {
                await Task.Delay(settings.DelayMs);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(settings.QueueName, false, settings.ConsumerTag, consumer, cancellationToken: stoppingToken);

        logger.LogInformation(
            "Consumer {ConsumerTag} bound to queue {Queue} (exchange {Exchange}, routing key {RoutingKey}, prefetch {Prefetch})",
            settings.ConsumerTag, settings.QueueName, settings.ExchangeName, settings.RoutingKey, settings.PrefetchCount);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
