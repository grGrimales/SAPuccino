namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Total de cafés em uma hora do dia (0–23, no fuso horário configurado),
/// agregando todos os dias do período. Útil para identificar horários de pico.
/// </summary>
public sealed record HourlyConsumption
{
    /// <summary>Hora do dia (0 a 23).</summary>
    public required int Hour { get; init; }

    /// <summary>Quantidade de cafés detectados nessa hora ao longo do período.</summary>
    public required int Count { get; init; }
}
