using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.Api.Infrastructure.Repositories;

public sealed class CoffeeEventRepository(AppDbContext dbContext) : ICoffeeEventRepository
{
    public async Task AddAsync(CoffeeEvent coffeeEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.CoffeeEvents.AddAsync(coffeeEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CoffeeEvent>> GetRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 200);

        return await dbContext.CoffeeEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DailyMetrics
            .AsNoTracking()
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }
}
