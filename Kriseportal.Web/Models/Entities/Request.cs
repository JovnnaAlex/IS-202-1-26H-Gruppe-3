using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models.Entities;

/// <summary>
/// En forespørsel fra en offentlig aktør til en ressursleverandør (modell 1.2).
/// Bare bekreftede forespørsler teller mot behovets antall.
/// </summary>
public class Request
{
    public int RequestId { get; set; }

    // Koblingen mellom behov og ressurs som forespørselen gjelder
    public int MatchId { get; set; }
    public Match? Match { get; set; }

    // Hvor mange enheter som etterspørres. Er 1 hvis gruppa velger én enhet per forespørsel.
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public RequestStatus Status { get; set; } = RequestStatus.Proposed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Settes når leverandøren bekrefter eller avslår
    public DateTime? RespondedAt { get; set; }

    // Sara: Valgfri melding til mottakeren ved godkjenning eller avslag.
    [MaxLength(2000)]
    public string? ResponseMessage { get; set; }
}
