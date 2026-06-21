using System.Text.Json.Serialization;

namespace AsterSupportAgent.Models;

/// <summary>
/// The single JSON action shape the model is instructed to emit.
/// Property name use [JsonPropertyName] (camelCase) to match the prompt contract exactly.
/// </summary>
public class AgentAction
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
