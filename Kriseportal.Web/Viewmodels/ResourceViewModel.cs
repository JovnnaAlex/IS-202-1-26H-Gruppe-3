using System.ComponentModel.DataAnnotations;
namespace Kriseportal.Web.Models
{
    public class ResourceViewModel
    {
            [Required(ErrorMessage = "Write the name of the resource")]
            public string Name { get; set; }
            [Required(ErrorMessage = "Write the type of resource")]    
            public string ResourceType { get; set; }
            public string? Description { get; set; }
            public bool Available { get; set; }
            [Required(ErrorMessage = "Write the latitude of the resource")]
            public double? Latitude { get; set; }
            [Required(ErrorMessage = "Write the longitude of the resource")]
            public double? Longitude { get; set; }
        }
    }

//endret av edgar lagt til required la til ? på double for å tillate nullverdier, ellers vil det kaste en feil hvis brukeren ikke fyller ut feltet. 

