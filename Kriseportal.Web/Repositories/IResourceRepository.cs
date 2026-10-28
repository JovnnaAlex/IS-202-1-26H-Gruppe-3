using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Web.Repositories;

// Interface som definerer hvilke databaseoperasjoner som skal være tilgjengelige for ressurser (Resource).
// Brukes av ResourceRepository og gjør det mulig å benytte dependency injection i controllerne.
public interface IResourceRepository
{
    // Henter alle registrerte ressurser fra databasen.
    Task<List<Resource>> GetAllAsync();

    // Henter én bestemt ressurs basert på ID. Returnerer null dersom ressursen ikke finnes.
    Task<Resource?> GetByIdAsync(int id);

    // Legger til en ny ressurs i databasen.
    Task AddAsync(Resource resource);
}