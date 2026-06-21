namespace AsterSupportAgent.DTOs;

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public List<TraceStep> Trace { get; set; } = [];
    public string SessionId { get; set; } = string.Empty;
}
