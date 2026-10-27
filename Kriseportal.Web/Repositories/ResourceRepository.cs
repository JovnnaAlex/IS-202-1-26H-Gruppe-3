using Kriseportal.Web.DataAccess;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kriseportal.Web.Repositories;

// Repository som håndterer databaseoperasjoner for ressurser (Resource).
// Implementerer IResourceRepository og bruker Entity Framework Core til å kommunisere med databasen.
public class ResourceRepository : IResourceRepository
{
    private readonly ApplicationDbContext _context;

    // Mottar ApplicationDbContext gjennom dependency injection.
    public ResourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Henter alle registrerte ressurser fra databasen.
    public async Task<List<Resource>> GetAllAsync()
    {
        return await _context.Resources.ToListAsync();
    }

    // Henter en bestemt ressurs basert på ID.
    // Returnerer null dersom ressursen ikke finnes.
    public async Task<Resource?> GetByIdAsync(int id)
    {
        return await _context.Resources.FindAsync(id);
    }

    // Legger til en ny ressurs og lagrer endringen i databasen.
    public async Task AddAsync(Resource resource)
    {
        await _context.Resources.AddAsync(resource);
        await _context.SaveChangesAsync();
    }
}