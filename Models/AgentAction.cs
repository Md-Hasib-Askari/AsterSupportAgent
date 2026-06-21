using System.Text.Json.Serialization;

namespace AsterSupportAgent.Models;

/// <summary>
/// The single JSON action shape the model is instructed to emit.
/// Property name use [JsonPropertyName] (camelCase) to match the prompt contract exactly.
/// </summary>
public class AgentAction
{
    [JsonPropertyName("action")]
    public AgentActionType Action { get; set; } = AgentActionType.NONE;

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentActionType
{
    NONE,
    SEARCH_KB,
    GET_ORDER_STATUS,
    CREATE_BOOKING_LINK,
    RESPOND,
}
