using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.Api.Infrastructure.Repositories;

public sealed class AvailabilityEventRepository(AppDbContext dbContext) : IAvailabilityEventRepository
{
    public async Task AddAsync(AvailabilityEvent availabilityEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.AvailabilityEvents.AddAsync(availabilityEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AvailabilityEvent?> GetLastAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AvailabilityEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AvailabilityEvent?> GetLastBeforeAsync(DateTime utc, CancellationToken cancellationToken = default)
    {
        return await dbContext.AvailabilityEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc < utc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailabilityEvent>> GetBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AvailabilityEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc < toUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}
