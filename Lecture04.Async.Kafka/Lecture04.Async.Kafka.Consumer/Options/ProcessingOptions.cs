namespace Lecture04.Async.Kafka.Consumer.Options;

public sealed class ProcessingOptions
{
    public string Topic { get; init; } = "cat-feedings";
    
    public int DelayMs { get; init; } = 100;
}
