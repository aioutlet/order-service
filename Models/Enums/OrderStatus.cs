namespace OrderService.Models.Enums;

public enum OrderStatus
{
    Created = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}
