namespace Kriseportal.Web.Models;

public class NeedViewModel
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "";

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}