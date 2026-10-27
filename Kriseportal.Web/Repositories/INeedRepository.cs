using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Repositories;

// Interface som definerer hvilke databaseoperasjoner som skal være tilgjengelige for behov (Need).
// Brukes av NeedRepository og gjør det mulig å benytte dependency injection i controllerne.
public interface INeedRepository
{
    // Henter alle registrerte behov fra databasen.
    Task<List<Need>> GetAllAsync();

    // Henter ett bestemt behov basert på ID. Returnerer null dersom behovet ikke finnes.
    Task<Need?> GetByIdAsync(int id);

    // Legger til et nytt behov i databasen.
    Task AddAsync(Need need);
}