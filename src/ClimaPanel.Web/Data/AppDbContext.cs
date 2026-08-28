using ClimaPanel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FavoriteCity> FavoriteCities => Set<FavoriteCity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FavoriteCity>(entity =>
        {
            entity.ToTable("FavoriteCities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.LocationId });
        });
    }
}
