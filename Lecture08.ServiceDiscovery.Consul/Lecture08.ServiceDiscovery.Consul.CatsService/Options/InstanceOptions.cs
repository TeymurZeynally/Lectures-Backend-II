namespace Lecture08.ServiceDiscovery.Consul.CatsService.Options;

public sealed class InstanceOptions
{
    public int Number { get; init; } = 1;

    public int DelayMs { get; init; } = 20;

    public string Name => $"cats-{Number}";

    public int Port => 5100 + Number;
}
