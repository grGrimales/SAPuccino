namespace CoffeeTracker.Api.Infrastructure.Mqtt;

public sealed class CoffeeDetector
{
    private const decimal DetectionThresholdInGrams = 5m;

    public bool HasCoffee(decimal weightInGrams)
    {
        return weightInGrams >= DetectionThresholdInGrams;
    }
}
