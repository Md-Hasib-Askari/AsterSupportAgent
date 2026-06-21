using System.Text.Json;
using AsterSupportAgent.Models;

namespace AsterSupportAgent.Services;

public interface IOrderService
{
    Order? GetOrderStatus(string orderId, out string? errorMessage);
}

public class OrderService : IOrderService
{
    private readonly Dictionary<string, Order> _orders;

    public OrderService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "orders.json");
        var json = File.ReadAllText(path);
        _orders =
            JsonSerializer.Deserialize<Dictionary<string, Order>>(json, JsonOptions.CaseInsensitive)
            ?? [];
    }

    public Order? GetOrderStatus(string orderId, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            errorMessage = "Order ID cannot be empty.";
            return null;
        }

        var normalized = orderId.Trim().ToUpperInvariant();
        if (_orders.TryGetValue(normalized, out var order))
        {
            errorMessage = null;
            return order;
        }

        errorMessage = $"Order ID '{orderId}' not found.";
        return null;
    }
}
