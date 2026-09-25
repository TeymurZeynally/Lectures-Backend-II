using Lecture08.ServiceDiscovery.Consul.CatsService.State;
using Microsoft.AspNetCore.Mvc;

namespace Lecture08.ServiceDiscovery.Consul.CatsService.Api.Health.Controllers;

// GET /health is the standard health check endpoint (MapHealthChecks in Program.cs).
// This controller only lets us break and fix the instance during the demo
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly InstanceState _state;

    public HealthController(InstanceState state)
    {
        _state = state;
    }

    [HttpPost("toggle")]
    public IActionResult Toggle()
    {
        _state.IsHealthy = !_state.IsHealthy;
        return Ok(_state.IsHealthy ? "healthy" : "unhealthy");
    }
}
