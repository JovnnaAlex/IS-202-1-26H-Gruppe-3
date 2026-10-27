using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Repositories;

// Repository som håndterer databaseoperasjoner for behov (Need).
// Implementerer INeedRepository og bruker Entity Framework Core til å kommunisere med databasen.
public class NeedRepository : INeedRepository
{
    private readonly ApplicationDbContext _context;

    // Mottar ApplicationDbContext gjennom dependency injection.
    public NeedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Henter alle registrerte behov fra databasen.
    public async Task<List<Need>> GetAllAsync()
    {
        return await _context.Needs.ToListAsync();
    }

    // Henter et bestemt behov basert på ID.
    // Returnerer null dersom behovet ikke finnes.
    public async Task<Need?> GetByIdAsync(int id)
    {
        return await _context.Needs.FindAsync(id);
    }

    // Legger til et nytt behov og lagrer endringen i databasen.
    public async Task AddAsync(Need need)
    {
        await _context.Needs.AddAsync(need);
        await _context.SaveChangesAsync();
    }
}