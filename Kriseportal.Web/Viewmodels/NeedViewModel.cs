using System.ComponentModel.DataAnnotations;
namespace Kriseportal.Web.Models
{
    public class NeedViewModel
    {
        [Required(ErrorMessage = "Write the type of need")]
        public string Type { get; set; }
        [Required(ErrorMessage = "Write the description of the need")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Choose the priority of the need")]
        public string Priority { get; set; }
        [Required(ErrorMessage = "Write the latitude of the need")]
        public double? Latitude { get; set; }
        [Required(ErrorMessage = "Write the longitude of the need")]
        public double? Longitude { get; set; }
    }
}
//endret av edgar lagt til required og ? på double for å tillate nullverdier, ellers vil det kaste en feil hvis brukeren ikke fyller ut feltet.
