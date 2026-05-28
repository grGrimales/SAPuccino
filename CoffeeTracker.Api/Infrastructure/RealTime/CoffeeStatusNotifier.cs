using CoffeeTracker.Api.Application.State;
using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CoffeeTracker.Api.Infrastructure.RealTime;

/// <summary>
/// Centraliza a atualização do estado e o envio em tempo real (fase 1). Tanto o assinante MQTT
/// quanto o endpoint de simulação registram um café por aqui, garantindo um único ponto de
/// verdade para o nome do evento SignalR e para a ordem (estado -> broadcast -> persistência).
/// </summary>
public sealed class CoffeeStatusNotifier(
    MachineStateTracker stateTracker,
    IHubContext<CoffeeHub> hubContext,
    IServiceScopeFactory scopeFactory,
    ILogger<CoffeeStatusNotifier> logger)
{
    /// <summary>Nome do evento SignalR que o frontend escuta.</summary>
    public const string StatusEvent = "statusUpdated";

    /// <summary>Registra um café: atualiza o estado, empurra via SignalR e persiste (melhor esforço).</summary>
    public async Task RegisterCoffeeAsync(DateTime occurredAtUtc, CancellationToken cancellationToken = default)
    {
        stateTracker.RecordCoffee(occurredAtUtc);

        // Tempo real primeiro: o FE atualiza mesmo que o banco esteja indisponível.
        await BroadcastStatusAsync(cancellationToken);
        await PersistEventAsync(occurredAtUtc, cancellationToken);
    }

    /// <summary>Envia o estado atual para todos os clientes conectados.</summary>
    public Task BroadcastStatusAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = stateTracker.GetSnapshot(DateTime.UtcNow);
        return hubContext.Clients.All.SendAsync(StatusEvent, snapshot, cancellationToken);
    }

    private async Task PersistEventAsync(DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        try
        {
            var coffeeEvent = new CoffeeEvent { OccurredAtUtc = occurredAtUtc };

            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICoffeeEventService>();
            await service.RegisterAsync(coffeeEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            // Não queremos perder o tempo real se a persistência falhar (ex.: banco indisponível).
            logger.LogError(ex, "Failed to persist coffee event. Live status was already updated.");
        }
    }
}
