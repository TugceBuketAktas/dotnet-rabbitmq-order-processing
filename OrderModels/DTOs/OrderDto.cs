namespace OrderModels.DTOs;

using OrderModels.Enums;

public class OrderDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
