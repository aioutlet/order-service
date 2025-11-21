namespace OrderService.Core.Models.DTOs;

public class CreateOrderDto
{
    public string CustomerId { get; set; } = string.Empty; // MongoDB ObjectId as string    
    // Customer information snapshot (captured at order creation time)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
        public List<CreateOrderItemDto> Items { get; set; } = new();
    public AddressDto ShippingAddress { get; set; } = new();
    public AddressDto BillingAddress { get; set; } = new();
    public string? Notes { get; set; }
}
