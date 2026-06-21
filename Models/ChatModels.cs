namespace AsterSupportAgent.Models;

public class ChatMessage
{
    public ChatMessageRole Role { get; set; } = ChatMessageRole.None;
    public string Content { get; set; } = string.Empty;
}

public record ChatMessageRole(string Value)
{
    public static readonly ChatMessageRole None = new(string.Empty);
    public static readonly ChatMessageRole System = new("system");
    public static readonly ChatMessageRole User = new("user");
    public static readonly ChatMessageRole Assistant = new("assistant");
}
