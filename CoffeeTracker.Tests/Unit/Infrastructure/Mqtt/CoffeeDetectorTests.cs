using CoffeeTracker.Api.Infrastructure.Mqtt;
using FluentAssertions;

namespace CoffeeTracker.Tests.Unit.Infrastructure.Mqtt;

public sealed class CoffeeDetectorTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(4.99, false)]
    [InlineData(5.00, true)]
    [InlineData(12.5, true)]
    public void HasCoffee_ShouldRespectThreshold(decimal weightInGrams, bool expected)
    {
        var sut = new CoffeeDetector();

        var result = sut.HasCoffee(weightInGrams);

        result.Should().Be(expected);
    }
}
