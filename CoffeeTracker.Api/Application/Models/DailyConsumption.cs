namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Total de cafés em um dia (no fuso horário configurado).
/// </summary>
public sealed record DailyConsumption
{
    /// <summary>Data local (yyyy-MM-dd).</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Quantidade de cafés detectados nesse dia.</summary>
    public required int Count { get; init; }
}
