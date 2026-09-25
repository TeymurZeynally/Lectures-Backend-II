using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using Lecture07.Realtime.WebSockets.ChatService.Api.Chat.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Lecture07.Realtime.WebSockets.ChatService.Api.Chat.Controllers;

[ApiController]
[Route("chat")]
public class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly ConcurrentDictionary<Guid, WebSocket> Sockets = new();
    private static readonly List<ChatMessage> History = new();
    private static readonly SemaphoreSlim BroadcastLock = new(1, 1);
    private static int _anonymousCounter;

    [HttpGet("messages")]
    public ActionResult<List<ChatMessage>> GetMessages()
    {
        lock (History)
        {
            return Ok(History.ToList());
        }
    }

    [HttpGet("ws")]
    public async Task Connect()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid();
        var nickname = $"Аноним {Interlocked.Increment(ref _anonymousCounter)}";

        await SendAsync(socket, new ChatEvent { Type = "welcome", Nickname = nickname });
        Sockets[id] = socket;

        try
        {
            var buffer = new byte[4096];

            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;
                }

                var request = JsonSerializer.Deserialize<SendMessageRequest>(buffer.AsSpan(0, result.Count), JsonOptions)!;
                var message = new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    Author = nickname,
                    Text = request.Text,
                    SentAt = DateTimeOffset.UtcNow
                };

                lock (History)
                {
                    History.Add(message);
                    if (History.Count > 50)
                    {
                        History.RemoveAt(0);
                    }
                }

                await BroadcastAsync(new ChatEvent { Type = "message", Message = message });
            }
        }
        finally
        {
            Sockets.TryRemove(id, out _);
        }
    }

    private static async Task BroadcastAsync(ChatEvent chatEvent)
    {
        await BroadcastLock.WaitAsync();
        try
        {
            foreach (var socket in Sockets.Values.Where(s => s.State == WebSocketState.Open))
            {
                await SendAsync(socket, chatEvent);
            }
        }
        finally
        {
            BroadcastLock.Release();
        }
    }

    private static Task SendAsync(WebSocket socket, ChatEvent chatEvent)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(chatEvent, JsonOptions);
        return socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
