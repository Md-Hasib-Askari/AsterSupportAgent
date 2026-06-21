using System.Text.Json.Serialization;

namespace AsterSupportAgent.Models;

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public DateTimeOffset? PlacedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public DateTimeOffset? EstimatedDelivery { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    [JsonIgnore]
    public bool Found => !string.IsNullOrEmpty(OrderId);
}
