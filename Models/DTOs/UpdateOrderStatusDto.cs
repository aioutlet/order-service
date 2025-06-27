using OrderService.Models.Enums;

namespace OrderService.Models.DTOs;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
    public string? Reason { get; set; }
}
