namespace Kriseportal.Web.Models.Entities;

/// <summary>
/// Kobler ett behov til én ressurs som er vurdert for det (modell 1.2).
/// Samme ressurs kan bare kobles til samme behov én gang (UNIQUE i databasen).
/// </summary>
public class Match
{
    public int MatchId { get; set; }

    // Behovet som trenger hjelp
    public int NeedId { get; set; }
    public Need? Need { get; set; }

    // Ressursen som er foreslått for behovet
    public int ResourceId { get; set; }
    public Resource? Resource { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Alle forespørsler som er sendt for denne koblingen
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}