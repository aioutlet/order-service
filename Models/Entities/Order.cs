using System.ComponentModel.DataAnnotations;
using OrderService.Models.Enums;

namespace OrderService.Models.Entities;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(24)] // MongoDB ObjectId is 24 characters when represented as hex string
    public string CustomerId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string OrderNumber { get; set; } = string.Empty;
    
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public ShippingStatus ShippingStatus { get; set; } = ShippingStatus.NotShipped;
    
    // Financial Details
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    [StringLength(3)]
    public string Currency { get; set; } = "USD";
    
    // Customer Information (snapshot at time of order)
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    
    // Addresses - using owned entity types (value objects)
    public Address ShippingAddress { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    
    // Payment Information
    public string? PaymentMethodId { get; set; } // Reference to payment method
    public string? PaymentProvider { get; set; } // stripe, paypal, etc.
    public string? PaymentTransactionId { get; set; }
    public string? PaymentReference { get; set; }
    
    // Shipping Information
    public string? ShippingMethod { get; set; } // standard, express, overnight
    public string? CarrierName { get; set; } // UPS, FedEx, USPS
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    
    // Business Logic
    public string? CouponCode { get; set; }
    public string? PromoCode { get; set; }
    public decimal TaxRate { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; } // For admin/support use
      
    // Cancellation
    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    
    // Returns/Refunds
    public bool IsReturnable { get; set; } = true;
    public DateTime? ReturnDeadline { get; set; }
    
    // Audit trail
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
    
    // Navigation property for order items
    public List<OrderItem> Items { get; set; } = new();
}
