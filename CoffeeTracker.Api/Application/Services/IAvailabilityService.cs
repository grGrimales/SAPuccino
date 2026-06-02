using CoffeeTracker.Api.Application.Models;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Registra as transições de disponibilidade da máquina (online/offline) e
/// gera relatórios de uptime a partir delas.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Registra uma transição. Faz dedup: se o estado for igual ao último registrado,
    /// nada é gravado (evita registros duplicados em reconexões seguidas).
    /// </summary>
    Task RecordTransitionAsync(bool isOnline, DateTime occurredAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monta o relatório de disponibilidade dos últimos <paramref name="days"/> dias
    /// (até o momento atual), agregando no fuso horário configurado.
    /// </summary>
    Task<AvailabilityReport> GetReportAsync(int days, DateTime nowUtc, CancellationToken cancellationToken = default);
}
