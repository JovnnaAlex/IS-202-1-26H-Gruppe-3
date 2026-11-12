using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Kriseportal.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Notification>(notification =>
        {
            notification.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            notification.HasIndex(n => new { n.UserId, n.IsRead });
        });

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

        modelBuilder.Entity<Match>(match =>
        {
            // Samme ressurs kan bare kobles til samme behov én gang
            match.HasIndex(m => new { m.NeedId, m.ResourceId }).IsUnique();

            // Slettes et behov eller en ressurs, slettes koblingene også.
            // Da kan Sara sin «Slett ressurs» fortsatt slette ressurser som er foreslått.
            match.HasOne(m => m.Need)
                .WithMany()
                .HasForeignKey(m => m.NeedId)
                .OnDelete(DeleteBehavior.Cascade);

            match.HasOne(m => m.Resource)
                .WithMany()
                .HasForeignKey(m => m.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Request>(request =>
        {
            // Lagres som tekst («Proposed»), så databasen er lesbar
            request.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Slettes koblingen, slettes forespørslene til den også
            request.HasOne(r => r.Match)
                .WithMany(m => m.Requests)
                .HasForeignKey(r => r.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Leverandøren henter ofte «alle som venter på svar»
            request.HasIndex(r => r.Status);

            request.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Request_Quantity", "`Quantity` >= 1");
                t.HasCheckConstraint("CK_Request_RespondedAt", "`RespondedAt` IS NULL OR `RespondedAt` >= `CreatedAt`");
            });
        });
    }
}