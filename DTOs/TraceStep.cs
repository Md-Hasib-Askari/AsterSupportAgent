using System.Text.Json.Serialization;

namespace AsterSupportAgent.DTOs;

public record TraceStep(int Step, TraceStepType Type, string? Raw = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TraceStepType
{
    NONE,
    TOOL_CALL,
    RESPOND,
    FALLBACK_RAW,
}
