namespace Lecture04.Async.Nats.Producer.Options;

public sealed class ProcessingOptions
{
    public string StreamName { get; init; } = "CAT_FEEDINGS";
    public string Subject { get; init; } = "cat.feedings";
    public int MessagesPerSecond { get; init; } = 50;
    public int CatCount { get; init; } = 20;
}
