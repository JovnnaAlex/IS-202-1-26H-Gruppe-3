using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class NeedViewModel
    {
        // Type behov er obligatorisk.
        [Required(ErrorMessage = "Du må oppgi type behov.")]
        public string Type { get; set; } = string.Empty;

        // Beskrivelse av behovet er obligatorisk.
        [Required(ErrorMessage = "Du må skrive en beskrivelse av behovet.")]
        public string Description { get; set; } = string.Empty;

        // Brukeren må velge prioritet.
        [Required(ErrorMessage = "Du må velge en prioritet.")]
        public string Priority { get; set; } = string.Empty;


        // Brukeren må velge en kategori.
        [Required(ErrorMessage = "Du må velge en kategori.")]
        public string Category { get; set; } = string.Empty;

        // Brukeren må velge en kommune.
        [Required(ErrorMessage = "Du må velge en kommune.")]
        public string Municipality { get; set; } = string.Empty;

        // Brukeren må velge når behovet starter.
        [Required(ErrorMessage = "Du må velge når behovet starter.")]
        public DateTime? NeedFrom { get; set; }

        // Brukeren må velge når behovet slutter.
        [Required(ErrorMessage = "Du må velge når behovet slutter.")]
        public DateTime? NeedTo { get; set; }

        // Breddegrad må registreres gjennom kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Latitude { get; set; }

        // Lengdegrad må registreres gjennom kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Longitude { get; set; }


    }
}
