namespace OrderService.Models.DTOs;

public class CreateOrderDto
{
    public string CustomerId { get; set; } = string.Empty; // MongoDB ObjectId as string
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public AddressDto ShippingAddress { get; set; } = new();
    public AddressDto BillingAddress { get; set; } = new();
    public string? Notes { get; set; }
}
