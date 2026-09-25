namespace Lecture08.ServiceDiscovery.Consul.CatsService.Options;

public sealed class ConsulOptions
{
    public string Url { get; init; } = "http://localhost:8500";

    public string ServiceName { get; init; } = "cats";
}
