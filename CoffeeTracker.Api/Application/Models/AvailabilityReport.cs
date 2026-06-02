namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Relatório de disponibilidade da máquina em um período: uptime, tempo online/offline,
/// número de quedas e a maior queda. Útil para manutenção e acompanhamento de SLA.
/// </summary>
public sealed record AvailabilityReport
{
    /// <summary>Início do período (UTC).</summary>
    public required DateTime FromUtc { get; init; }

    /// <summary>Fim do período (UTC; normalmente "agora").</summary>
    public required DateTime ToUtc { get; init; }

    /// <summary>Número de dias considerados no período.</summary>
    public required int Days { get; init; }

    /// <summary>Estado atual: "online" ou "offline".</summary>
    public required string CurrentState { get; init; }

    /// <summary>Desde quando (UTC) o estado atual vigora, ou null se nunca houve registro.</summary>
    public DateTime? CurrentSinceUtc { get; init; }

    /// <summary>Percentual de tempo online no período (0–100, 2 casas).</summary>
    public required double UptimePercent { get; init; }

    /// <summary>Tempo total online no período, em segundos.</summary>
    public required long TotalOnlineSeconds { get; init; }

    /// <summary>Tempo total offline no período, em segundos.</summary>
    public required long TotalOfflineSeconds { get; init; }

    /// <summary>Número de quedas (períodos offline) dentro da janela.</summary>
    public required int Outages { get; init; }

    /// <summary>Duração da maior queda no período, em segundos.</summary>
    public required long LongestOutageSeconds { get; init; }

    /// <summary>Início (UTC) da maior queda, ou null se não houve quedas.</summary>
    public DateTime? LongestOutageStartUtc { get; init; }

    /// <summary>Transições ocorridas dentro do período (para uma linha do tempo).</summary>
    public required IReadOnlyList<AvailabilityTransition> Transitions { get; init; }
}
