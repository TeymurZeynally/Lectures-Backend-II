namespace Lecture04.Async.Kafka.Consumer.Options;

public sealed class ConsumerOptions
{
    public string Topic { get; init; } = "cat-feedings";
    public int ProcessingDelayMs { get; init; } = 100;
}
