namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Uma transição de disponibilidade no período (para montar uma linha do tempo no frontend).
/// </summary>
public sealed record AvailabilityTransition
{
    /// <summary>Momento (UTC) da transição.</summary>
    public required DateTime OccurredAtUtc { get; init; }

    /// <summary>True = ficou online; False = ficou offline.</summary>
    public required bool IsOnline { get; init; }
}
