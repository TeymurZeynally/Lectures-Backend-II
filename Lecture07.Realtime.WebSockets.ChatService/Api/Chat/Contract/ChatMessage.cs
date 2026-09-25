namespace Lecture07.Realtime.WebSockets.ChatService.Api.Chat.Contract;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}
