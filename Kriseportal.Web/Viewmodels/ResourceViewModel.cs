using System.ComponentModel.DataAnnotations;
using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Models;

public class ResourceViewModel : IValidatableObject
{
    // Navn på ressursen.
    [Required(ErrorMessage = "Du må oppgi navn på ressursen.")]
    [MaxLength(150, ErrorMessage = "Navnet kan ikke være lengre enn 150 tegn.")]
    public string Name { get; set; } = string.Empty;

    // Ressurskategori fra databasen.
    [Required(ErrorMessage = "Du må velge en ressurskategori.")]
    public int? CategoryId { get; set; }

    // Valgfri beskrivelse.
    public string? Description { get; set; }

    // Antall og måleenhet.
    [Range(1, int.MaxValue, ErrorMessage = "Antall må være minst 1.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Du må oppgi en enhet.")]
    [MaxLength(20, ErrorMessage = "Enheten kan ikke være lengre enn 20 tegn.")]
    public string Unit { get; set; } = "stk";

    // Tilgjengelighetsstatus.
    [EnumDataType(typeof(ResourceAvailability),
        ErrorMessage = "Du må velge en gyldig tilgjengelighetsstatus.")]
    public ResourceAvailability Availability { get; set; }
        = ResourceAvailability.Available;

    // Valgfri tilgjengelighetsperiode.
    [DataType(DataType.Date)]
    public DateTime? AvailableFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? AvailableTo { get; set; }

    // Kommune.
    [Required(ErrorMessage = "Du må oppgi kommune.")]
    [MaxLength(100, ErrorMessage = "Kommunenavnet kan ikke være lengre enn 100 tegn.")]
    public string Municipality { get; set; } = string.Empty;

    // Kontaktinformasjon.
    [Required(ErrorMessage = "Du må oppgi kontaktperson.")]
    [MaxLength(100, ErrorMessage = "Navnet kan ikke være lengre enn 100 tegn.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Du må oppgi telefonnummer.")]
    [MaxLength(20, ErrorMessage = "Telefonnummeret kan ikke være lengre enn 20 tegn.")]
    [RegularExpression(@"^\+?[\d\s]{8,20}$",
        ErrorMessage = "Oppgi et gyldig telefonnummer.")]
    public string ContactPhone { get; set; } = string.Empty;

    // Geografisk plassering.
    [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
    [Range(-90, 90, ErrorMessage = "Ugyldig breddegrad.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Du må velge en plassering på kartet.")]
    [Range(-180, 180, ErrorMessage = "Ugyldig lengdegrad.")]
    public double? Longitude { get; set; }

    // Validerer at sluttdato ikke kommer før startdato.
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (AvailableFrom.HasValue &&
            AvailableTo.HasValue &&
            AvailableTo.Value < AvailableFrom.Value)
        {
            yield return new ValidationResult(
                "Sluttdato kan ikke være før startdato.",
                new[] { nameof(AvailableTo) });
        }
    }
}