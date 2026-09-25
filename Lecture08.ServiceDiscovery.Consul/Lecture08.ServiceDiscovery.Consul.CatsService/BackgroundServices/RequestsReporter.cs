using Lecture08.ServiceDiscovery.Consul.CatsService.Options;
using Lecture08.ServiceDiscovery.Consul.CatsService.State;
using Microsoft.Extensions.Options;

namespace Lecture08.ServiceDiscovery.Consul.CatsService.BackgroundServices;

// Once a second prints how many requests this instance has handled
public sealed class RequestsReporter(
    InstanceState state,
    IOptions<InstanceOptions> options,
    ILogger<RequestsReporter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation(
                "{Instance} handled {Count} req/s ({Health})",
                options.Value.Name, state.ResetRequestCount(), state.IsHealthy ? "healthy" : "unhealthy");
        }
    }
}
