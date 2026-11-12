using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Models;

public class ResourceRequestViewModel
{
    public Resource Resource { get; set; } = null!;

    public int AvailableQuantity { get; set; }

    public List<Need> Needs { get; set; } = new();
}