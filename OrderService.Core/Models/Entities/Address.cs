using System.ComponentModel.DataAnnotations;

namespace OrderService.Core.Models.Entities;

/// <summary>
/// Address value object for embedding in Order entity
/// </summary>
public class Address
{
    [Required]
    [StringLength(100)]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string AddressLine2 { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(2)]
    public string Country { get; set; } = "US";
}
