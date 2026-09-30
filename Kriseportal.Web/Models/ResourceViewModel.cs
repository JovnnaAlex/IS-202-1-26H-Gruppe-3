namespace Kriseportal.Web.Models;

public class ResourceViewModel
{
    public string Name { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string Description { get; set; } = "";

    public bool Available { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}