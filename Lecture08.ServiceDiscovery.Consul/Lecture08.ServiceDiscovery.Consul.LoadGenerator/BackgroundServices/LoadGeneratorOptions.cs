namespace Lecture08.ServiceDiscovery.Consul.LoadGenerator.BackgroundServices;

public sealed class LoadGeneratorOptions
{
    // ServerSide - send everything to HAProxy (Url), it finds the instances in Consul.
    // ClientSide - ask Consul for the instances and call them directly, without HAProxy
    public string Mode { get; init; } = "ServerSide";

    public string Url { get; init; } = "http://localhost:8080/cats/1";

    public string ConsulUrl { get; init; } = "http://localhost:8500";

    public int RequestsPerSecond { get; init; } = 100;

    public int MaxConnections { get; init; } = 1000;
}
