using Lecture08.ServiceDiscovery.Consul.CatsService.State;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lecture08.ServiceDiscovery.Consul.CatsService.HealthChecks;

public sealed class InstanceHealthCheck(InstanceState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(state.IsHealthy
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Broken via POST /health/toggle"));
    }
}
