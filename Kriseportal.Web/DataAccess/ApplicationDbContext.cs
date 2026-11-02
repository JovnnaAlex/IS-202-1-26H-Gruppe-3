using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Kriseportal.Web.DataAccess;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Hver DbSet blir én tabell
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // To kategorier kan ikke ha samme navn
        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Need>(need =>
        {
            // Lagres som tekst («Acute») i stedet for tall, så databasen er lesbar
            need.Property(n => n.Priority).HasConversion<string>().HasMaxLength(20);
            need.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

            // En kategori som er i bruk, kan ikke slettes
            need.HasOne(n => n.Category)
                .WithMany(c => c.Needs)
                .HasForeignKey(n => n.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Kobler behovet til den offentlige aktøren som registrerte det
            need.HasOne(n => n.PublicAuthority)
                .WithMany()
                .HasForeignKey(n => n.PublicAuthorityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indekser på feltene vi filtrerer mest på
            need.HasIndex(n => n.Status);
            need.HasIndex(n => n.Municipality);

            need.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Need_Quantity", "`Quantity` >= 1");
                t.HasCheckConstraint("CK_Need_Period", "`NeededTo` IS NULL OR `NeededTo` >= `NeededFrom`");
                t.HasCheckConstraint("CK_Need_Latitude", "`Latitude` BETWEEN -90 AND 90");
                t.HasCheckConstraint("CK_Need_Longitude", "`Longitude` BETWEEN -180 AND 180");
            });
        });

        modelBuilder.Entity<Resource>(resource =>
        {
            resource.Property(r => r.Availability).HasConversion<string>().HasMaxLength(20);

            resource.HasOne(r => r.Category)
                .WithMany(c => c.Resources)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Kobler ressursen til ressursleverandøren som eier den
            resource.HasOne(r => r.Provider)
                .WithMany()
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            resource.HasIndex(r => r.Municipality);
            resource.HasIndex(r => r.Availability);

            resource.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Resource_Quantity", "`Quantity` >= 1");
                t.HasCheckConstraint("CK_Resource_Period", "`AvailableFrom` IS NULL OR `AvailableTo` IS NULL OR `AvailableTo` >= `AvailableFrom`");
                t.HasCheckConstraint("CK_Resource_Latitude", "`Latitude` BETWEEN -90 AND 90");
                t.HasCheckConstraint("CK_Resource_Longitude", "`Longitude` BETWEEN -180 AND 180");
            });
        });
    }
}