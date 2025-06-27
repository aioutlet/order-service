using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Entities;

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [StringLength(24)] // MongoDB ObjectId is 24 characters when represented as hex string
    public string ProductId { get; set; } = string.Empty;
    
    [Required]
    public string ProductName { get; set; } = string.Empty;
    
    public string? ProductSku { get; set; }
    public string? ProductDescription { get; set; }
    
    // Product attributes snapshot at time of order (from Product Service)
    public string? ProductCategory { get; set; }
    public string? ProductBrand { get; set; }
    
    // Selected variant details (from product.variants)
    public string? ProductColor { get; set; }
    public string? ProductSize { get; set; }
    
    // Product physical attributes (from product.attributes)
    public string? ProductWeight { get; set; }
    public string? ProductDimensions { get; set; }
    
    // Pricing and quantities
    public decimal UnitPrice { get; set; }
    public decimal OriginalPrice { get; set; } // Before any discounts
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCostPerItem { get; set; }
    
    // Discount information
    public string? DiscountType { get; set; } // percentage, fixed, bogo
    public string? DiscountCode { get; set; }
    public decimal DiscountPercentage { get; set; }
    
    // Customization (order-specific)
    public string? CustomizationDetails { get; set; } // engraving, personalization
    public string? SpecialInstructions { get; set; }
    
    // Gift options
    public bool IsGiftWrapped { get; set; }
    public decimal GiftWrapCost { get; set; }
    public string? GiftWrapType { get; set; }
    
    // Returns and refunds (order-specific tracking)
    public bool IsReturnable { get; set; } = true;
    public string? ReturnReason { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? ReturnCondition { get; set; }
    
    // Audit trail
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property back to order
    public Order Order { get; set; } = null!;
}
