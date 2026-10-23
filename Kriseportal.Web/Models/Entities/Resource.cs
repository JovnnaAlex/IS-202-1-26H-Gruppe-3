using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Models.Entities;

public class Resource
{
    public int ResourceId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "stk";

    public ResourceAvailability Availability { get; set; } = ResourceAvailability.Available;

    // Tomme betyr «tilgjengelig når som helst»
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }

    [Precision(9, 6)]
    public decimal Latitude { get; set; }

    [Precision(9, 6)]
    public decimal Longitude { get; set; }

    [Required, MaxLength(100)]
    public string Municipality { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Ressursleverandøren som eier ressursen. Kobles til ApplicationUser i B02.
    [Required]
    public string ProviderId { get; set; } = string.Empty;
}