using System.Globalization;
using System.Text;
using CoffeeTracker.Api.Application.Services;
using CoffeeTracker.Api.Application.State;
using CoffeeTracker.Api.Infrastructure.RealTime;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace CoffeeTracker.Api.Infrastructure.Mqtt;

/// <summary>
/// Assinante MQTT (fase 1). A máquina publica o indicador H1: quando passa de 0 para 1
/// significa que começou a preparar um café. Detectamos essa borda de subida e delegamos
/// no <see cref="CoffeeStatusNotifier"/> (atualiza estado, empurra via SignalR e persiste).
/// </summary>
public sealed class MqttSubscriberService(
    IConfiguration configuration,
    MachineStateTracker stateTracker,
    CoffeeStatusNotifier notifier,
    IServiceScopeFactory scopeFactory,
    ILogger<MqttSubscriberService> logger) : BackgroundService
{
    // -1 = desconhecido (evita contar um café falso na primeira mensagem ao iniciar).
    private int _lastH1 = -1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var broker = configuration["Mqtt:Broker"] ?? "localhost";
        var port = configuration.GetValue("Mqtt:Port", 1883);
        var topic = configuration["Mqtt:Topic"] ?? "coffee/events";
        var username = configuration["Mqtt:Username"];
        var password = configuration["Mqtt:Password"];

        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateManagedMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += args =>
            HandleMessageAsync(args, stoppingToken);

        mqttClient.ConnectedAsync += async _ =>
        {
            stateTracker.SetBrokerConnected(true);
            logger.LogInformation("Connected to MQTT broker {Broker}:{Port}", broker, port);
            await RecordAvailabilityAsync(true, stoppingToken);
            await notifier.BroadcastStatusAsync(stoppingToken);
        };

        mqttClient.DisconnectedAsync += async args =>
        {
            stateTracker.SetBrokerConnected(false);
            logger.LogWarning(args.Exception, "Disconnected from MQTT broker. Reason: {Reason}", args.Reason);
            await RecordAvailabilityAsync(false, stoppingToken);
            await notifier.BroadcastStatusAsync(stoppingToken);
        };

        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId($"coffee-tracker-api-{Guid.NewGuid():N}");

        if (!string.IsNullOrWhiteSpace(username))
        {
            clientOptionsBuilder.WithCredentials(username, password);
        }

        var managedOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptionsBuilder.Build())
            .Build();

        await mqttClient.StartAsync(managedOptions);

        await mqttClient.SubscribeAsync([
            new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .Build()
        ]);

        logger.LogInformation("Subscribed to MQTT topic {Topic}", topic);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // A aplicação está sendo encerrada.
        }

        await mqttClient.StopAsync();
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs args, CancellationToken stoppingToken)
    {
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

        if (!TryParseH1(payload, out var h1, out var occurredAtUtc))
        {
            // Não é um apontamento de status H1 (pode ser outro tipo de dado no mesmo topic).
            return;
        }

        logger.LogInformation("H1 status received: {H1} at {OccurredAtUtc}", h1, occurredAtUtc);

        // Um café = borda de subida de H1 (0 -> 1).
        var isNewCoffee = _lastH1 == 0 && h1 == 1;
        _lastH1 = h1;

        if (!isNewCoffee)
        {
            return;
        }

        logger.LogInformation("Coffee detected from MQTT (H1 0->1) at {OccurredAtUtc}", occurredAtUtc);
        await notifier.RegisterCoffeeAsync(occurredAtUtc, stoppingToken);
    }

    /// <summary>
    /// Persiste uma transição de disponibilidade (melhor esforço). O serviço faz dedup,
    /// então reconexões repetidas não geram registros duplicados.
    /// </summary>
    private async Task RecordAvailabilityAsync(bool isOnline, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var availability = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
            await availability.RecordTransitionAsync(isOnline, DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist availability transition (isOnline={IsOnline}).", isOnline);
        }
    }

    /// <summary>
    /// Lê o indicador H1 do payload de DADOSAPONTAMENTO, no formato
    /// "&lt;timestamp&gt;|&lt;cod&gt;|&lt;H1&gt;|&lt;rótulo&gt;|&lt;descrição&gt;|" (ex.:
    /// "2026-05-27T10:47:54.000-03:00|99|1|Status (H1)|Machine status signal|").
    /// Confirma que é um apontamento de status H1 e usa o timestamp real da máquina.
    /// Também aceita um payload "0"/"1" direto (topic /IoT/SAACE/H1) como alternativa.
    /// </summary>
    private static bool TryParseH1(string payload, out int h1, out DateTime occurredAtUtc)
    {
        h1 = 0;
        occurredAtUtc = DateTime.UtcNow;

        var trimmed = payload.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        if (trimmed.Contains('|'))
        {
            var parts = trimmed.Split('|');

            // Garante que é o apontamento de status H1 (o rótulo contém "H1").
            var isH1Record = parts.Length > 3 && parts[3].Contains("H1", StringComparison.OrdinalIgnoreCase);
            if (!isH1Record || !int.TryParse(parts[2].Trim(), out var value))
            {
                return false;
            }

            h1 = value == 0 ? 0 : 1;

            if (DateTimeOffset.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
            {
                occurredAtUtc = timestamp.UtcDateTime;
            }

            return true;
        }

        // Alternativa: payload "0"/"1" direto.
        if (int.TryParse(trimmed, out var direct) && direct is 0 or 1)
        {
            h1 = direct;
            return true;
        }

        return false;
    }
}
