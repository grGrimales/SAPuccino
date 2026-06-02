using CoffeeTracker.Api.Application.Models;
using CoffeeTracker.Api.Domain.Interfaces;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Calcula relatórios de consumo agregando os eventos de café no fuso horário
/// configurado. A agregação é feita em memória: o volume de cafés é baixo e
/// isso evita acoplar a lógica de fuso ao provedor do banco.
/// </summary>
public sealed class ConsumptionReportService(
    ICoffeeEventRepository repository,
    TimeZoneInfo timeZone) : IConsumptionReportService
{
    // Limite de segurança para o período solicitado (evita varreduras enormes).
    private const int MaxDays = 90;

    public async Task<ConsumptionReport> GetReportAsync(
        int days,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedDays = Math.Clamp(days, 1, MaxDays);

        var today = LocalDateOf(nowUtc);
        var fromDate = today.AddDays(-(normalizedDays - 1));

        // Janela UTC [início do primeiro dia local, início do dia seguinte ao último).
        var fromUtc = StartOfLocalDayUtc(fromDate);
        var toUtc = StartOfLocalDayUtc(today.AddDays(1));

        var events = await repository.GetBetweenAsync(fromUtc, toUtc, cancellationToken);

        // Converte cada evento para o horário local antes de agrupar.
        var locals = events
            .Select(e => TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc), timeZone))
            .ToList();

        var daily = BuildDailySeries(locals, fromDate, normalizedDays);
        var hourly = BuildHourlyDistribution(locals);

        var total = locals.Count;
        var busiestDay = total == 0
            ? null
            : daily.OrderByDescending(x => x.Count).ThenByDescending(x => x.Date).First();
        var peakHour = total == 0
            ? null
            : hourly.OrderByDescending(x => x.Count).ThenBy(x => x.Hour).First();

        return new ConsumptionReport
        {
            FromDate = fromDate,
            ToDate = today,
            Days = normalizedDays,
            TotalCoffees = total,
            AveragePerDay = Math.Round((double)total / normalizedDays, 2),
            BusiestDay = busiestDay,
            PeakHour = peakHour,
            Daily = daily,
            Hourly = hourly,
            LastUsedUtc = events.Count == 0 ? null : events.Max(e => e.OccurredAtUtc)
        };
    }

    // Série por dia com todos os dias do período presentes (dias sem café = 0).
    private static IReadOnlyList<DailyConsumption> BuildDailySeries(
        IEnumerable<DateTime> locals, DateOnly fromDate, int days)
    {
        var perDate = locals
            .GroupBy(l => DateOnly.FromDateTime(l))
            .ToDictionary(g => g.Key, g => g.Count());

        var series = new List<DailyConsumption>(days);
        for (var i = 0; i < days; i++)
        {
            var date = fromDate.AddDays(i);
            series.Add(new DailyConsumption { Date = date, Count = perDate.GetValueOrDefault(date) });
        }

        return series;
    }

    // Distribuição por hora do dia com as 24 posições sempre presentes.
    private static IReadOnlyList<HourlyConsumption> BuildHourlyDistribution(IEnumerable<DateTime> locals)
    {
        var perHour = locals
            .GroupBy(l => l.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var distribution = new List<HourlyConsumption>(24);
        for (var hour = 0; hour < 24; hour++)
        {
            distribution.Add(new HourlyConsumption { Hour = hour, Count = perHour.GetValueOrDefault(hour) });
        }

        return distribution;
    }

    private DateOnly LocalDateOf(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone));

    private DateTime StartOfLocalDayUtc(DateOnly localDate)
    {
        var startLocal = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone);
    }
}
