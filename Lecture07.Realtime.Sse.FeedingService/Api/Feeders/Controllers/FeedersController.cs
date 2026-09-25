using System.Text.Encodings.Web;
using System.Text.Json;
using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Contract;
using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Controllers;

[ApiController]
[Route("feeders")]
public class FeedersController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IFeedersService _feedersService;

    public FeedersController(IFeedersService feedersService)
    {
        _feedersService = feedersService;
    }

    [HttpGet]
    public ActionResult<List<Feeder>> GetAll()
    {
        return Ok(_feedersService.GetAll());
    }

    [HttpPost("{id:int}/refill")]
    public ActionResult<Feeder> Refill(int id)
    {
        var feeder = _feedersService.Refill(id);
        return feeder is null ? NotFound() : Ok(feeder);
    }

    // SSE is just a long-lived HTTP response with Content-Type text/event-stream.
    // Every event is a few "field: value" lines followed by an empty line.
    // .NET 10 can do the same with TypedResults.ServerSentEvents, here it is written by hand to show the format.
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";

        await foreach (var feeder in _feedersService.SubscribeAsync(cancellationToken))
        {
            var json = JsonSerializer.Serialize(feeder, JsonOptions);

            await Response.WriteAsync($"event: feeder-updated\ndata: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
