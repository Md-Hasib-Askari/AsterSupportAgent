using System.Text.Json.Serialization;

namespace AsterSupportAgent.Models;

public class ChatMessage
{
    public ChatMessageRole Role { get; set; } = ChatMessageRole.NONE;
    public string Content { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatMessageRole
{
    NONE,
    SYSTEM,
    USER,
    ASSISTANT,
}
