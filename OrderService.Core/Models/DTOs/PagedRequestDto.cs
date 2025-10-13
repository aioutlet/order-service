using System.ComponentModel.DataAnnotations;

namespace OrderService.Core.Models.DTOs;

/// <summary>
/// Base pagination request DTO
/// </summary>
public class PagedRequestDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Skip count for pagination
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}
