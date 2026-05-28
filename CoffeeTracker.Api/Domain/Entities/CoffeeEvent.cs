namespace CoffeeTracker.Api.Domain.Entities;

public sealed class CoffeeEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public decimal WeightInGrams { get; init; }
    public bool HasCoffee { get; init; }
}
