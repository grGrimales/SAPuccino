namespace CoffeeTracker.Api.Domain.Entities;

/// <summary>
/// Representa um café preparado pela máquina, detectado quando o indicador H1 vai de 0 para 1.
/// Cada registro já é um café; não há peso nem flag de detecção (fase 1).
/// </summary>
public sealed class CoffeeEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
