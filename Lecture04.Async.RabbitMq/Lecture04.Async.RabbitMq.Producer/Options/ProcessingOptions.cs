namespace Lecture04.Async.RabbitMq.Producer.Options;

public sealed class ProcessingOptions
{
    public string ExchangeName { get; init; } = "cat-feedings-exchange";
    public string RoutingKey { get; init; } = "feeding";
    public string QueueName { get; init; } = "cat-feedings";
    public int MessagesPerSecond { get; init; } = 50;
    public int CatCount { get; init; } = 20;
}
