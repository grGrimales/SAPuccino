namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Snapshot do estado da máquina de café consumido pelo frontend (fase 1).
/// </summary>
public sealed record CoffeeStatus
{
    /// <summary>"online" se o backend está conectado ao broker MQTT, "offline" caso contrário.</summary>
    public required string MachineState { get; init; }

    /// <summary>Quantidade de cafés detectados no dia atual (fuso horário configurado).</summary>
    public required int CoffeesToday { get; init; }

    /// <summary>Momento (UTC) do último café detectado, ou null se ainda não há registros.</summary>
    public DateTime? LastUsedUtc { get; init; }
}
