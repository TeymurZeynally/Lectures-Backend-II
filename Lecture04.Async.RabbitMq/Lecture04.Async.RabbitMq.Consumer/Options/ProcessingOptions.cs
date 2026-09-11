namespace Lecture04.Async.RabbitMq.Consumer.Options;

public sealed class ProcessingOptions
{
    public string ExchangeName { get; init; } = "cat-feedings-exchange";
    public string RoutingKey { get; init; } = "feeding";
    public string QueueName { get; init; } = "cat-feedings";
    public string ConsumerTag { get; init; } = "consumer-1";
    public int PrefetchCount { get; init; } = 1;
    public int DelayMs { get; init; } = 100;
}
