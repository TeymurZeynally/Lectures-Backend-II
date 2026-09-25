using System.Net.Http.Json;
using System.Text.Json;
using Lecture08.ServiceDiscovery.Consul.CatsService.Options;
using Microsoft.Extensions.Options;

namespace Lecture08.ServiceDiscovery.Consul.CatsService.BackgroundServices;

public sealed class ConsulRegistration(
    IHttpClientFactory httpClientFactory,
    IOptions<ConsulOptions> consulOptions,
    IOptions<InstanceOptions> instanceOptions,
    ILogger<ConsulRegistration> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consul = consulOptions.Value;
        var instance = instanceOptions.Value;

        var registration = new
        {
            ID = instance.Name,
            Name = consul.ServiceName,
            Address = "127.0.0.1",
            Port = instance.Port,
            Check = new
            {
                HTTP = $"http://127.0.0.1:{instance.Port}/health",
                Interval = "1s",
                DeregisterCriticalServiceAfter = "1m"
            }
        };

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        logger.LogInformation("Registering {Instance} as service {Service} in Consul", instance.Name, consul.ServiceName);

        do
        {
            var response = await httpClientFactory.CreateClient().PutAsJsonAsync(
                $"{consul.Url}/v1/agent/service/register", registration, JsonSerializerOptions.Default, stoppingToken);
            response.EnsureSuccessStatusCode();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        var instanceName = instanceOptions.Value.Name;

        await httpClientFactory.CreateClient().PutAsync(
            $"{consulOptions.Value.Url}/v1/agent/service/deregister/{instanceName}", null, cancellationToken);

        logger.LogInformation("Deregistered {Instance} from Consul", instanceName);
    }
}
