namespace OrderService.Models.DTOs;

public class OrderItemResponseDto
{
    public Guid Id { get; set; }
    public string ProductId { get; set; } = string.Empty; // MongoDB ObjectId as string
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
