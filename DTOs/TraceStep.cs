using System.Text.Json.Serialization;

namespace AsterSupportAgent.DTOs;

public class TraceStep
{
    public int Step { get; set; }
    public TraceStepType Type { get; set; } = TraceStepType.NONE;
    public string? Raw { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TraceStepType
{
    NONE,
    TOOL_CALL,
    RESPOND,
    FALLBACK_RAW,
}
