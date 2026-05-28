using System.Text;
using System.Text.Json;
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
    ILogger<MqttSubscriberService> logger) : BackgroundService
{
    private int _lastH1;

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
            await notifier.BroadcastStatusAsync(stoppingToken);
        };

        mqttClient.DisconnectedAsync += async args =>
        {
            stateTracker.SetBrokerConnected(false);
            logger.LogWarning(args.Exception, "Disconnected from MQTT broker. Reason: {Reason}", args.Reason);
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

        if (!TryParseH1(payload, out var h1))
        {
            logger.LogWarning("Could not read H1 from MQTT payload: {Payload}", payload);
            return;
        }

        // Um café = borda de subida de H1 (0 -> 1).
        var isNewCoffee = _lastH1 == 0 && h1 == 1;
        _lastH1 = h1;

        if (!isNewCoffee)
        {
            return;
        }

        var occurredAtUtc = DateTime.UtcNow;
        logger.LogInformation("Coffee detected from MQTT (H1 0->1) at {OccurredAtUtc}", occurredAtUtc);
        await notifier.RegisterCoffeeAsync(occurredAtUtc, stoppingToken);
    }

    /// <summary>
    /// Extrai o indicador H1 do payload. Tolerante porque ainda não fixamos o formato real:
    /// aceita "0"/"1" direto ou um JSON que contenha uma propriedade "H1". AJUSTAR com uma amostra real.
    /// </summary>
    private static bool TryParseH1(string payload, out int h1)
    {
        h1 = 0;
        var trimmed = payload.Trim();

        if (int.TryParse(trimmed, out var direct) && direct is 0 or 1)
        {
            h1 = direct;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!prop.Name.Equals("H1", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var num))
                    {
                        h1 = num == 0 ? 0 : 1;
                        return true;
                    }

                    if (prop.Value.ValueKind == JsonValueKind.String &&
                        int.TryParse(prop.Value.GetString(), out var str))
                    {
                        h1 = str == 0 ? 0 : 1;
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Não é um JSON válido.
        }

        return false;
    }
}
