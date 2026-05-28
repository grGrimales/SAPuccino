using System.Globalization;
using System.Text;
using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace CoffeeTracker.Api.Infrastructure.Mqtt;

public sealed class MqttSubscriberService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    CoffeeDetector detector,
    ILogger<MqttSubscriberService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var broker = configuration["Mqtt:Broker"] ?? "localhost";
        var port = configuration.GetValue("Mqtt:Port", 1883);
        var topic = configuration["Mqtt:Topic"] ?? "coffee/events";
        var username = configuration["Mqtt:Username"];
        var password = configuration["Mqtt:Password"];

        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateManagedMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += async args =>
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            if (!decimal.TryParse(payload, NumberStyles.Number, CultureInfo.InvariantCulture, out var weightInGrams))
            {
                logger.LogWarning("Invalid MQTT payload received: {Payload}", payload);
                return;
            }

            var coffeeEvent = new CoffeeEvent
            {
                WeightInGrams = weightInGrams,
                HasCoffee = detector.HasCoffee(weightInGrams),
                OccurredAtUtc = DateTime.UtcNow
            };

            using var scope = scopeFactory.CreateScope();
            var coffeeEventService = scope.ServiceProvider.GetRequiredService<ICoffeeEventService>();

            await coffeeEventService.RegisterAsync(coffeeEvent, stoppingToken);

            logger.LogInformation(
                "Coffee event registered from MQTT. Weight: {WeightInGrams}, HasCoffee: {HasCoffee}",
                coffeeEvent.WeightInGrams,
                coffeeEvent.HasCoffee);
        };

        mqttClient.ConnectedAsync += _ =>
        {
            logger.LogInformation("Connected to MQTT broker {Broker}:{Port}", broker, port);
            return Task.CompletedTask;
        };

        mqttClient.DisconnectedAsync += args =>
        {
            logger.LogWarning(
                args.Exception,
                "Disconnected from MQTT broker. Reason: {Reason}",
                args.Reason);

            return Task.CompletedTask;
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
            // Application is shutting down.
        }

        await mqttClient.StopAsync();
    }
}
