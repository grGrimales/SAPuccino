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

    public async Task<int> CountBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CoffeeEvents
            .AsNoTracking()
            .CountAsync(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CoffeeEvent>> GetBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CoffeeEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLastOccurredAtUtcAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CoffeeEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => (DateTime?)x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
