namespace Lecture04.Async.Kafka.Producer.Options;

public sealed class ProcessingOptions
{
    public string Topic { get; init; } = "cat-feedings";

    public int MessagesPerSecond { get; init; } = 50;

    public int CatCount { get; init; } = 20;
}
