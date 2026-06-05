namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Indicadores do dia derivados do histórico de cafés (sem sensores extras).
/// Todas as agregações usam o fuso horário configurado.
/// </summary>
public sealed record DashboardSummary
{
    /// <summary>Data local de referência (hoje).</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Cafés preparados hoje.</summary>
    public required int CoffeesToday { get; init; }

    /// <summary>Cafés preparados ontem (mesmo intervalo de dia local).</summary>
    public required int CoffeesYesterday { get; init; }

    /// <summary>Variação percentual de hoje em relação a ontem; null se ontem não teve cafés.</summary>
    public double? ChangeVsYesterdayPercent { get; init; }

    /// <summary>Média de cafés por hora desde o primeiro café do dia.</summary>
    public required double AveragePerHour { get; init; }

    /// <summary>Intervalo médio entre cafés de hoje, em segundos; null se houve menos de 2 cafés.</summary>
    public int? AverageIntervalSeconds { get; init; }

    /// <summary>Momento (UTC) do último café registrado, ou null.</summary>
    public DateTime? LastUsedUtc { get; init; }

    /// <summary>Segundos desde o último café, ou null se não há registros.</summary>
    public long? SecondsSinceLastCoffee { get; init; }

    /// <summary>Horário de pico de hoje (hora local com mais cafés), ou null se não houve cafés.</summary>
    public HourlyConsumption? PeakHourToday { get; init; }
}
