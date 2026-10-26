using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class NeedViewModel
    {
        // Type behov er obligatorisk.
        [Required(ErrorMessage = "Du må oppgi type behov.")]
        public string Type { get; set; }

        // Beskrivelse av behovet er obligatorisk.
        [Required(ErrorMessage = "Du må skrive en beskrivelse av behovet.")]
        public string Description { get; set; }

        // Brukeren må velge prioritet.
        [Required(ErrorMessage = "Du må velge en prioritet.")]
        public string Priority { get; set; }

        // Breddegrad må registreres gjennom kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Latitude { get; set; }

        // Lengdegrad må registreres gjennom kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Longitude { get; set; }
    }
}
