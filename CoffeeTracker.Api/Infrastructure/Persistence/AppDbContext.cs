using CoffeeTracker.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CoffeeEvent> CoffeeEvents => Set<CoffeeEvent>();
    public DbSet<DailyMetric> DailyMetrics => Set<DailyMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoffeeEvent>(entity =>
        {
            entity.ToTable("coffee_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WeightInGrams).HasPrecision(10, 2);
            entity.Property(x => x.OccurredAtUtc).IsRequired();
        });

        modelBuilder.Entity<DailyMetric>(entity =>
        {
            entity.ToTable("daily_metrics");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
        });
    }
}
