using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.DTOs;

/// <summary>
/// Address DTO for API requests and responses
/// </summary>
public class AddressDto
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
