using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Models;

public class MatchingSuggestionsViewModel
{
    public int NeedId { get; set; }

    public Need Need { get; set; } = null!;

    public List<Resource> Resources { get; set; } = new();

    // Alle forespørsler til dette behovet, inkludert svar.
    public List<Request> Requests { get; set; } = new();

    // Ledig antall per ressurs etter bekreftede forespørsler.
    public Dictionary<int, int> AvailableQuantities { get; set; } = new();

    public int ConfirmedQuantity { get; set; }

    public int RemainingQuantity =>
        Math.Max(0, Need.Quantity - ConfirmedQuantity);
}