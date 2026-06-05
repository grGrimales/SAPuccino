using CoffeeTracker.Api.Application.Models;
using CoffeeTracker.Api.Domain.Interfaces;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Deriva os KPIs do dia a partir dos eventos de café (contagem, comparativo com ontem,
/// média horária, intervalo médio e horário de pico), tudo no fuso configurado.
/// </summary>
public sealed class DashboardService(
    ICoffeeEventRepository repository,
    TimeZoneInfo timeZone) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var today = LocalDateOf(nowUtc);
        var todayStartUtc = StartOfLocalDayUtc(today);
        var tomorrowStartUtc = StartOfLocalDayUtc(today.AddDays(1));
        var yesterdayStartUtc = StartOfLocalDayUtc(today.AddDays(-1));

        var coffeesToday = await repository.CountBetweenAsync(todayStartUtc, tomorrowStartUtc, cancellationToken);
        var coffeesYesterday = await repository.CountBetweenAsync(yesterdayStartUtc, todayStartUtc, cancellationToken);
        var todaysEvents = await repository.GetBetweenAsync(todayStartUtc, tomorrowStartUtc, cancellationToken);
        var lastUsedUtc = await repository.GetLastOccurredAtUtcAsync(cancellationToken);

        // Comparativo vs ontem (null quando ontem não teve cafés, para não dividir por zero).
        double? changeVsYesterday = coffeesYesterday > 0
            ? Math.Round((coffeesToday - coffeesYesterday) / (double)coffeesYesterday * 100, 0)
            : null;

        // Locais ordenados de hoje (para média horária, intervalo e pico).
        var locals = todaysEvents
            .Select(e => TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc), timeZone))
            .OrderBy(d => d)
            .ToList();

        // Média por hora desde o primeiro café (piso de 1h para não inflar no início do dia).
        double averagePerHour = 0;
        if (todaysEvents.Count > 0)
        {
            var firstUtc = todaysEvents.Min(e => e.OccurredAtUtc);
            var hours = Math.Max((nowUtc - firstUtc).TotalHours, 1.0);
            averagePerHour = Math.Round(coffeesToday / hours, 1);
        }

        // Intervalo médio entre cafés de hoje (precisa de pelo menos 2).
        int? averageIntervalSeconds = null;
        if (locals.Count >= 2)
        {
            var spanSeconds = (locals[^1] - locals[0]).TotalSeconds;
            averageIntervalSeconds = (int)Math.Round(spanSeconds / (locals.Count - 1));
        }

        // Tempo desde o último café.
        long? secondsSinceLastCoffee = lastUsedUtc.HasValue
            ? (long)Math.Max((nowUtc - lastUsedUtc.Value).TotalSeconds, 0)
            : null;

        // Horário de pico de hoje.
        HourlyConsumption? peakHourToday = null;
        if (locals.Count > 0)
        {
            var top = locals
                .GroupBy(d => d.Hour)
                .Select(g => new HourlyConsumption { Hour = g.Key, Count = g.Count() })
                .OrderByDescending(h => h.Count)
                .ThenBy(h => h.Hour)
                .First();
            peakHourToday = top;
        }

        return new DashboardSummary
        {
            Date = today,
            CoffeesToday = coffeesToday,
            CoffeesYesterday = coffeesYesterday,
            ChangeVsYesterdayPercent = changeVsYesterday,
            AveragePerHour = averagePerHour,
            AverageIntervalSeconds = averageIntervalSeconds,
            LastUsedUtc = lastUsedUtc,
            SecondsSinceLastCoffee = secondsSinceLastCoffee,
            PeakHourToday = peakHourToday
        };
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
