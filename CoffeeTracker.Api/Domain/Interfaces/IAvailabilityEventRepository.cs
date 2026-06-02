using CoffeeTracker.Api.Domain.Entities;

namespace CoffeeTracker.Api.Domain.Interfaces;

public interface IAvailabilityEventRepository
{
    Task AddAsync(AvailabilityEvent availabilityEvent, CancellationToken cancellationToken = default);

    /// <summary>Última transição registrada (para dedup e estado atual), ou null se não há nenhuma.</summary>
    Task<AvailabilityEvent?> GetLastAsync(CancellationToken cancellationToken = default);

    /// <summary>Última transição estritamente anterior a <paramref name="utc"/> (estado no início de uma janela).</summary>
    Task<AvailabilityEvent?> GetLastBeforeAsync(DateTime utc, CancellationToken cancellationToken = default);

    /// <summary>Transições no intervalo [fromUtc, toUtc), em ordem cronológica.</summary>
    Task<IReadOnlyList<AvailabilityEvent>> GetBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
