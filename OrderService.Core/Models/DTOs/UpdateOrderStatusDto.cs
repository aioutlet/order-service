using OrderService.Core.Models.Enums;

namespace OrderService.Core.Models.DTOs;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
    public string? Reason { get; set; }
}
