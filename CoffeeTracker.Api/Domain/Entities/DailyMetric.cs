namespace CoffeeTracker.Api.Domain.Entities;

public sealed class DailyMetric
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public int TotalReadings { get; set; }
    public int CoffeeDetections { get; set; }
}
