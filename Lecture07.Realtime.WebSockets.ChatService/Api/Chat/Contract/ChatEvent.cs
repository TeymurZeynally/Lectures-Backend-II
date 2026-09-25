namespace Lecture07.Realtime.WebSockets.ChatService.Api.Chat.Contract;

// Server -> client. Type is "welcome" (Nickname is set) or "message" (Message is set)
public class ChatEvent
{
    public string Type { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public ChatMessage? Message { get; set; }
}
