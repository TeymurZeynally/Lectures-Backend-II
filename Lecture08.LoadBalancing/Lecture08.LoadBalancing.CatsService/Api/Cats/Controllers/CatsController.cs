using Lecture08.LoadBalancing.CatsService.Api.Cats.Contract;
using Lecture08.LoadBalancing.CatsService.Options;
using Lecture08.LoadBalancing.CatsService.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lecture08.LoadBalancing.CatsService.Api.Cats.Controllers;

[ApiController]
[Route("cats")]
public class CatsController : ControllerBase
{
    private static readonly string[] Names = ["Барсик", "Шлёпа", "Блинк", "Бендер", "Кот Окак", "Кот в сапогах"];

    private readonly InstanceState _state;
    private readonly InstanceOptions _options;

    public CatsController(InstanceState state, IOptions<InstanceOptions> options)
    {
        _state = state;
        _options = options.Value;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatResponse>> GetById(int id)
    {
        _state.CountRequest();
        await Task.Delay(_options.DelayMs);

        if (!_state.IsHealthy)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok(new CatResponse { Id = id, Name = Names[id % Names.Length], Instance = _options.Name });
    }
}
