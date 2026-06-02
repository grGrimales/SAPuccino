using CoffeeTracker.Api.Application.Services;
using CoffeeTracker.Api.Application.State;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Infrastructure.RealTime;

namespace CoffeeTracker.Api.Endpoints;

public static class CoffeeEndpoints
{
    public static IEndpointRouteBuilder MapCoffeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coffee")
            .WithTags("Coffee");

        // Fase 1: estado vivo da máquina (online/offline, cafés de hoje, última utilização).
        group.MapGet("/status", (MachineStateTracker tracker) =>
            Results.Ok(tracker.GetSnapshot(DateTime.UtcNow)))
            .WithName("GetCoffeeStatus");

        // Histórico recente de cafés.
        group.MapGet("/events", async (ICoffeeEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetRecentAsync(cancellationToken: cancellationToken)))
            .WithName("GetCoffeeEvents");

        // Relatório de consumo (analytics): cafés por dia, horários de pico e KPIs do período.
        // Ex.: GET /api/coffee/reports?days=7  (padrão 7 dias, limitado a 90).
        group.MapGet("/reports", async (
            IConsumptionReportService reports,
            int? days,
            CancellationToken cancellationToken) =>
            Results.Ok(await reports.GetReportAsync(days ?? 7, DateTime.UtcNow, cancellationToken)))
            .WithName("GetConsumptionReport");

        // Relatório de disponibilidade: uptime, tempo online/offline, quedas e maior queda.
        // Ex.: GET /api/coffee/availability?days=7  (padrão 7 dias, limitado a 90).
        group.MapGet("/availability", async (
            IAvailabilityService availability,
            int? days,
            CancellationToken cancellationToken) =>
            Results.Ok(await availability.GetReportAsync(days ?? 7, DateTime.UtcNow, cancellationToken)))
            .WithName("GetAvailabilityReport");

        // Simula um café (útil para a demo sem a máquina real): atualiza estado e empurra via SignalR.
        group.MapPost("/simulate", async (CoffeeStatusNotifier notifier, CancellationToken cancellationToken) =>
        {
            await notifier.RegisterCoffeeAsync(DateTime.UtcNow, cancellationToken);
            return Results.Accepted();
        })
        .WithName("SimulateCoffee");

        return app;
    }
}
