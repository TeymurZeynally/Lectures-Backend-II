namespace Lecture07.Realtime.WebSockets.ChatService.Api.Chat.Contract;

// Client -> server
public class SendMessageRequest
{
    public string Text { get; set; } = string.Empty;
}
