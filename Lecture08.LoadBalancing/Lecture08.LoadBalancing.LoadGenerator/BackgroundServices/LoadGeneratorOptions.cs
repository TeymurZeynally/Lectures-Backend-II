namespace Lecture08.LoadBalancing.LoadGenerator.BackgroundServices;

public sealed class LoadGeneratorOptions
{
    public string Url { get; init; } = "http://localhost:8080/cats/1";

    public int RequestsPerSecond { get; init; } = 100;

    public int MaxConnections { get; init; } = 1000;
}
