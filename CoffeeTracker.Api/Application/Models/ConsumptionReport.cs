namespace CoffeeTracker.Api.Application.Models;

/// <summary>
/// Relatório de consumo de café em um período, consumido pelo frontend para
/// montar gráficos (consumo por dia, horários de pico) e indicadores (KPIs).
/// Todas as agregações usam o fuso horário configurado.
/// </summary>
public sealed record ConsumptionReport
{
    /// <summary>Primeira data local do período (inclusiva).</summary>
    public required DateOnly FromDate { get; init; }

    /// <summary>Última data local do período (inclusiva; normalmente "hoje").</summary>
    public required DateOnly ToDate { get; init; }

    /// <summary>Número de dias considerados no período.</summary>
    public required int Days { get; init; }

    /// <summary>Total de cafés no período.</summary>
    public required int TotalCoffees { get; init; }

    /// <summary>Média de cafés por dia no período (arredondada para 2 casas).</summary>
    public required double AveragePerDay { get; init; }

    /// <summary>Dia com maior consumo no período, ou null se não houve cafés.</summary>
    public DailyConsumption? BusiestDay { get; init; }

    /// <summary>Horário de pico (hora do dia com maior consumo), ou null se não houve cafés.</summary>
    public HourlyConsumption? PeakHour { get; init; }

    /// <summary>Série de consumo por dia (inclui dias sem café, com Count = 0).</summary>
    public required IReadOnlyList<DailyConsumption> Daily { get; init; }

    /// <summary>Distribuição por hora do dia (24 posições, 0–23, inclui horas com Count = 0).</summary>
    public required IReadOnlyList<HourlyConsumption> Hourly { get; init; }

    /// <summary>Momento (UTC) do último café no período, ou null se não houve.</summary>
    public DateTime? LastUsedUtc { get; init; }
}
