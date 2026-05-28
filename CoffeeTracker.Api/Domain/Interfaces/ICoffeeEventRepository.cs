using CoffeeTracker.Api.Domain.Entities;

namespace CoffeeTracker.Api.Domain.Interfaces;

public interface ICoffeeEventRepository
{
    Task AddAsync(CoffeeEvent coffeeEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CoffeeEvent>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<int> CountBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastOccurredAtUtcAsync(CancellationToken cancellationToken = default);
}
