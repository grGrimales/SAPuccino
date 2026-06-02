using CoffeeTracker.Api.Application.Models;
using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;

namespace CoffeeTracker.Api.Application.Services;

/// <summary>
/// Calcula disponibilidade a partir do log de transições. Os tempos online/offline,
/// o número de quedas e a maior queda são contabilizados dentro da janela; o "desde
/// quando" do estado atual usa o timestamp real (que pode ser anterior à janela).
/// </summary>
public sealed class AvailabilityService(
    IAvailabilityEventRepository repository,
    TimeZoneInfo timeZone) : IAvailabilityService
{
    private const int MaxDays = 90;

    public async Task RecordTransitionAsync(
        bool isOnline,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        // Dedup: só registra quando o estado realmente muda.
        var last = await repository.GetLastAsync(cancellationToken);
        if (last is not null && last.IsOnline == isOnline)
        {
            return;
        }

        await repository.AddAsync(
            new AvailabilityEvent { OccurredAtUtc = occurredAtUtc, IsOnline = isOnline },
            cancellationToken);
    }

    public async Task<AvailabilityReport> GetReportAsync(
        int days,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedDays = Math.Clamp(days, 1, MaxDays);

        var today = LocalDateOf(nowUtc);
        var fromDate = today.AddDays(-(normalizedDays - 1));
        var fromUtc = StartOfLocalDayUtc(fromDate);
        var toUtc = nowUtc > fromUtc ? nowUtc : fromUtc;

        var initial = await repository.GetLastBeforeAsync(fromUtc, cancellationToken);
        var transitions = await repository.GetBetweenAsync(fromUtc, toUtc, cancellationToken);

        // Define o início da contabilização (evita medir tempo "não observado"):
        // - com dado antes da janela: parte do início da janela com aquele estado;
        // - sem dado antes, mas com transições: parte da primeira observação;
        // - sem nenhum dado: não há o que medir.
        bool state;
        DateTime cursor;
        DateTime? currentSince;
        IReadOnlyList<AvailabilityEvent> walk;

        if (initial is not null)
        {
            state = initial.IsOnline;
            cursor = fromUtc;
            currentSince = initial.OccurredAtUtc;
            walk = transitions;
        }
        else if (transitions.Count > 0)
        {
            state = transitions[0].IsOnline;
            cursor = transitions[0].OccurredAtUtc;
            currentSince = transitions[0].OccurredAtUtc;
            walk = transitions.Skip(1).ToList();
        }
        else
        {
            return new AvailabilityReport
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Days = normalizedDays,
                CurrentState = "offline",
                CurrentSinceUtc = null,
                UptimePercent = 0,
                TotalOnlineSeconds = 0,
                TotalOfflineSeconds = 0,
                Outages = 0,
                LongestOutageSeconds = 0,
                LongestOutageStartUtc = null,
                Transitions = []
            };
        }

        double onlineSeconds = 0;
        double offlineSeconds = 0;
        var outages = 0;
        double longestOutage = 0;
        DateTime? longestOutageStart = null;

        void Accumulate(DateTime segmentEnd)
        {
            var seconds = (segmentEnd - cursor).TotalSeconds;
            if (seconds <= 0)
            {
                return;
            }

            if (state)
            {
                onlineSeconds += seconds;
            }
            else
            {
                offlineSeconds += seconds;
                outages++;
                if (seconds > longestOutage)
                {
                    longestOutage = seconds;
                    longestOutageStart = cursor;
                }
            }
        }

        foreach (var transition in walk)
        {
            Accumulate(transition.OccurredAtUtc);
            cursor = transition.OccurredAtUtc;
            state = transition.IsOnline;
            currentSince = transition.OccurredAtUtc;
        }

        // Segmento final, do último ponto até "agora".
        Accumulate(toUtc);

        var total = onlineSeconds + offlineSeconds;
        var uptime = total > 0 ? Math.Round(onlineSeconds / total * 100, 2) : 0;

        return new AvailabilityReport
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Days = normalizedDays,
            CurrentState = state ? "online" : "offline",
            CurrentSinceUtc = currentSince,
            UptimePercent = uptime,
            TotalOnlineSeconds = (long)Math.Round(onlineSeconds),
            TotalOfflineSeconds = (long)Math.Round(offlineSeconds),
            Outages = outages,
            LongestOutageSeconds = (long)Math.Round(longestOutage),
            LongestOutageStartUtc = longestOutageStart,
            Transitions = transitions
                .Select(t => new AvailabilityTransition { OccurredAtUtc = t.OccurredAtUtc, IsOnline = t.IsOnline })
                .ToList()
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
