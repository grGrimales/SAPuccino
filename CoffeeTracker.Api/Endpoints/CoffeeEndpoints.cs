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
