using OpenTelemetry.Resources;
using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models.Entities;

public class Category
{
    public int CategoryId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Alle behov og ressurser i denne kategorien
    public List<Need> Needs { get; set; } = new();
    public List<Resource> Resources { get; set; } = new();
}
