using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;

namespace CoffeeTracker.Api.Endpoints;

public static class CoffeeEndpoints
{
    public static IEndpointRouteBuilder MapCoffeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coffee")
            .WithTags("Coffee");

        group.MapGet("/events", async (ICoffeeEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetRecentAsync(cancellationToken: cancellationToken)))
            .WithName("GetCoffeeEvents");

        group.MapGet("/metrics/daily", async (ICoffeeEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDailyMetricsAsync(cancellationToken)))
            .WithName("GetDailyMetrics");

        group.MapPost("/events", async (
            CoffeeEvent coffeeEvent,
            ICoffeeEventService service,
            CancellationToken cancellationToken) =>
        {
            await service.RegisterAsync(coffeeEvent, cancellationToken);
            return Results.Accepted("/api/coffee/events", coffeeEvent);
        })
        .WithName("RegisterCoffeeEvent");

        return app;
    }
}
