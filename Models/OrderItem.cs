namespace AsterSupportAgent.Models;

public class OrderItem
{
    public string Name { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Price { get; set; }
}
