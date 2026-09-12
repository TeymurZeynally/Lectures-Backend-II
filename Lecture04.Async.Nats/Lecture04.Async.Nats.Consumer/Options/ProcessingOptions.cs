namespace Lecture04.Async.Nats.Consumer.Options;

public sealed class ProcessingOptions
{
    public string StreamName { get; init; } = "CAT_FEEDINGS";
    public string Subject { get; init; } = "cat.feedings";
    public string DurableConsumerName { get; init; } = "cat-feeding-processors";
    public string QueueGroup { get; init; } = "cat-feeding-processors";
    public string InstanceName { get; init; } = "consumer-1";
    public int PrefetchCount { get; init; } = 1;
    public int DelayMs { get; init; } = 100;
}
