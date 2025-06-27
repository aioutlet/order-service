namespace OrderService.Models.DTOs;

public class CreateOrderItemDto
{
    public string ProductId { get; set; } = string.Empty; // MongoDB ObjectId as string
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
