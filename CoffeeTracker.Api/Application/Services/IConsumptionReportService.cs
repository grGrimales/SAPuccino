using CoffeeTracker.Api.Application.Models;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Gera relatórios de consumo de café a partir do histórico persistido.
/// </summary>
public interface IConsumptionReportService
{
    /// <summary>
    /// Monta o relatório dos últimos <paramref name="days"/> dias (incluindo o dia atual),
    /// agregando no fuso horário configurado.
    /// </summary>
    /// <param name="days">Quantidade de dias do período (será limitada a um intervalo seguro).</param>
    /// <param name="nowUtc">Momento de referência em UTC (normalmente DateTime.UtcNow).</param>
    Task<ConsumptionReport> GetReportAsync(int days, DateTime nowUtc, CancellationToken cancellationToken = default);
}
