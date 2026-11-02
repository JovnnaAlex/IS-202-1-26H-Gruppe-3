using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class NeedViewModel
    {
        // Tittel på behovet er obligatorisk, maks 150 tegn.
        [Required(ErrorMessage = "Du må oppgi en tittel.")]
        [StringLength(150, ErrorMessage = "Tittelen kan ikke være lengre enn 150 tegn.")]
        public string Title { get; set; } = string.Empty;
        
        // Brukeren må velge en kategori som finnes i databasen.
        [Range(1, int.MaxValue, ErrorMessage = "Du må velge en kategori.")]
        public int CategoryId { get; set; }

        // Kategorinavnet brukes til visning på detaljsiden.
        public string? CategoryName { get; set; }
        
        // Antall ressurser som trengs, minimum 1.
        [Range(1, int.MaxValue, ErrorMessage = "Antall må være minst 1.")]
        public int Quantity { get; set; } = 1;
        
        // Måleenheten er obligatorisk og kan være maks 20 tegn.
        [Required(ErrorMessage = "Du må oppgi en måleenhet.")]
        [StringLength(20, ErrorMessage = "Måleenheten kan ikke være lengre enn 20 tegn.")]
        public string Unit { get; set; } = string.Empty;
        
        // Tidspunktet behovet starter.
        [Required(ErrorMessage = "Du må oppgi når behovet starter.")]
        public DateTime? NeededFrom { get; set; }

        // Tidspunktet behovet eventuelt avsluttes.
        public DateTime? NeededTo { get; set; }
        
        // Kommunen der behovet oppstår.
        [Required(ErrorMessage = "Du må oppgi en kommune.")]
        [StringLength(100, ErrorMessage = "Kommunen kan ikke være lengre enn 100 tegn.")]
        public string Municipality { get; set; } = string.Empty;
        
        // Navnet på kontaktpersonen for behovet.
        [Required(ErrorMessage = "Du må oppgi en kontaktperson.")]
        [StringLength(100, ErrorMessage = "Kontaktpersonens navn kan ikke være lengre enn 100 tegn.")]
        public string ContactName { get; set; } = string.Empty;
        
        // Telefonnummer til kontaktpersonen.
        [Required(ErrorMessage = "Du må oppgi et telefonnummer.")]
        [StringLength(20, ErrorMessage = "Telefonnummeret kan ikke være lengre enn 20 tegn.")]
        [Phone(ErrorMessage = "Du må oppgi et gyldig telefonnummer.")]
        public string ContactPhone { get; set; } = string.Empty;
        
        // Beskrivelse av behovet er obligatorisk.
        [Required(ErrorMessage = "Du må skrive en beskrivelse av behovet.")]
        public string Description { get; set; } = string.Empty;

        // Brukeren må velge prioritet.
        [Required(ErrorMessage = "Du må velge en prioritet.")]
        public string Priority { get; set; } = string.Empty;

        // Breddegrad må være mellom -90 og 90 grader.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        [Range(-90.0, 90.0, ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
        public double? Latitude { get; set; }

        // Lengdegrad må være mellom -180 og 180 grader.
        [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
        [Range(-180.0, 180.0, ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
        public double? Longitude { get; set; }
    }
}
