using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;

namespace CoffeeTracker.Api.Application.Services;

public sealed class CoffeeEventService(ICoffeeEventRepository repository) : ICoffeeEventService
{
    public Task RegisterAsync(CoffeeEvent coffeeEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coffeeEvent);
        return repository.AddAsync(coffeeEvent, cancellationToken);
    }

    public Task<IReadOnlyCollection<CoffeeEvent>> GetRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return repository.GetRecentAsync(take, cancellationToken);
    }

    public Task<IReadOnlyCollection<DailyMetric>> GetDailyMetricsAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetDailyMetricsAsync(cancellationToken);
    }
}
