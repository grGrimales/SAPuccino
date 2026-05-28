using CoffeeTracker.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CoffeeEvent> CoffeeEvents => Set<CoffeeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoffeeEvent>(entity =>
        {
            entity.ToTable("coffee_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OccurredAtUtc).IsRequired();
            entity.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
