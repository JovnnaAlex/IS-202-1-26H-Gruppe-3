using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class ResourceViewModel
    {
        // Navn på ressursen er obligatorisk.
        [Required(ErrorMessage = "Du må oppgi navn på ressursen.")]
        public string Name { get; set; }

        // Brukeren må velge en ressurstype.
        [Required(ErrorMessage = "Du må velge en ressurstype.")]
        public string ResourceType { get; set; }

        // Beskrivelse er valgfritt.
        public string? Description { get; set; }

        // Angir om ressursen er tilgjengelig.
        public bool Available { get; set; }

        // Brukeren må velge en plassering på kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Latitude { get; set; }

        // Lengdegrad registreres automatisk fra kartet.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        public double? Longitude { get; set; }
    }
}