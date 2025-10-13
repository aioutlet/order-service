using OrderService.Core.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Core.Models.DTOs;

/// <summary>
/// Order query parameters for filtering and pagination
/// </summary>
public class OrderQueryDto : PagedRequestDto
{
    /// <summary>
    /// Filter by order status
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Filter by customer ID
    /// </summary>
    [StringLength(24, MinimumLength = 24, ErrorMessage = "Customer ID must be exactly 24 characters")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Filter by order date from (inclusive)
    /// </summary>
    public DateTime? OrderDateFrom { get; set; }

    /// <summary>
    /// Filter by order date to (inclusive)
    /// </summary>
    public DateTime? OrderDateTo { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public OrderSortBy SortBy { get; set; } = OrderSortBy.OrderDateDesc;
}

/// <summary>
/// Order sorting options
/// </summary>
public enum OrderSortBy
{
    OrderDateAsc,
    OrderDateDesc,
    TotalAmountAsc,
    TotalAmountDesc,
    StatusAsc,
    StatusDesc
}
