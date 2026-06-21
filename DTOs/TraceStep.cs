namespace AsterSupportAgent.DTOs;

public class TraceStep
{
    public int Step { get; set; }
    public TraceStepType Type { get; set; } = TraceStepType.None;
}

public record TraceStepType(string Value)
{
    public static readonly TraceStepType None = new(string.Empty);
    public static readonly TraceStepType ToolCall = new("tool_call");
    public static readonly TraceStepType Respond = new("respond");
    public static readonly TraceStepType FallbackRaw = new("fallback_raw");
}
