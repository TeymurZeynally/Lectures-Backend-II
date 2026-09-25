using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Lecture07.Realtime.WebRtc.SignalingService.Api.Signaling.Contract;
using Lecture07.Realtime.WebRtc.SignalingService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lecture07.Realtime.WebRtc.SignalingService.Api.Signaling.Controllers;

[ApiController]
[Route("signaling")]
public class SignalingController : ControllerBase
{
    // room name -> peers in the room (at most two)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> Rooms = new();

    private readonly IceServersOptions _iceServers;

    public SignalingController(IOptions<IceServersOptions> iceServers)
    {
        _iceServers = iceServers.Value;
    }

    // TURN credentials are given out by the server, so they are not hardcoded in the page
    [HttpGet("ice-servers")]
    public ActionResult<List<IceServer>> GetIceServers(string mode = "stun")
    {
        return Ok(mode == "turn" ? _iceServers.Turn : _iceServers.Stun);
    }

    [HttpGet("rooms/{room}")]
    public async Task Join(string room)
    {
        var peers = Rooms.GetOrAdd(room, _ => new());

        if (!HttpContext.WebSockets.IsWebSocketRequest || peers.Count >= 2)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();

        foreach (var peer in peers.Values)
        {
            await peer.SendAsync(Encoding.UTF8.GetBytes("""{"type":"peer-joined"}"""), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        peers[id] = socket;

        try
        {
            var buffer = new byte[16 * 1024];

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;
                }

                foreach (var peer in peers.Where(p => p.Key != id).Select(p => p.Value))
                {
                    await peer.SendAsync(buffer.AsMemory(0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                }
            }
        }
        finally
        {
            peers.TryRemove(id, out _);
        }
    }
}
