namespace CoffeeTracker.Api.Domain.Entities;

/// <summary>
/// Representa uma transição de disponibilidade da máquina: o momento em que ela
/// ficou online (conectada ao broker MQTT) ou offline. A partir da sequência
/// dessas transições é possível calcular uptime, número de quedas e duração das quedas.
/// </summary>
public sealed class AvailabilityEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Momento (UTC) em que a transição ocorreu.</summary>
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>True = máquina ficou online; False = ficou offline.</summary>
    public bool IsOnline { get; init; }
}
