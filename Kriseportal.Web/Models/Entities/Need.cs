using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Models.Entities;

public class Need
{
    public int NeedId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Fremmednøkkel til Category + navigasjon
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public NeedPriority Priority { get; set; } = NeedPriority.Normal;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Required, MaxLength(20)]
    public string Unit { get; set; } = "stk";

    public DateTime NeededFrom { get; set; }
    public DateTime? NeededTo { get; set; }

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

    public NeedStatus Status { get; set; } = NeedStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Den offentlige aktøren som registrerte behovet.
    // Kobles til ApplicationUser i B02, når Backend har laget den i B06.
    [Required]
    public string PublicAuthorityId { get; set; } = string.Empty;
}