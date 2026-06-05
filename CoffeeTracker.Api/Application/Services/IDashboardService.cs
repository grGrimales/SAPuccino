using CoffeeTracker.Api.Application.Models;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Calcula indicadores do dia (KPIs) a partir do histórico de cafés.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
