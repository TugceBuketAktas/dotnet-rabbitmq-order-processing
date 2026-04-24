using System.ComponentModel.DataAnnotations;

namespace OrderModels.DTOs;

public class CreateOrderDto
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemDto> Items { get; set; } = new();
}
