using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models.Entities;

// Hvilken type offentlig aktør en bruker representerer.
// Verdiene følger kolonnen Agency i Database/kriseberedskap.sql og lagres som tekst.
public enum Agency
{
    [Display(Name = "Stat")] Stat,
    [Display(Name = "Kommune")] Kommune,
    [Display(Name = "Politi")] Politi,
    [Display(Name = "Brann")] Brann,
    [Display(Name = "Helse")] Helse,
    [Display(Name = "Sivilforsvaret")] Sivilforsvaret,
    [Display(Name = "Forsvaret")] Forsvaret,
    [Display(Name = "Heimevernet")] Heimevernet
}
