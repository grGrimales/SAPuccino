using CoffeeTracker.Api.Application.Services;
using CoffeeTracker.Api.Domain.Entities;
using CoffeeTracker.Api.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace CoffeeTracker.Tests.Unit.Application.Services;

public sealed class CoffeeEventServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldDelegateToRepository()
    {
        var repository = Substitute.For<ICoffeeEventRepository>();
        var sut = new CoffeeEventService(repository);
        var coffeeEvent = new CoffeeEvent { WeightInGrams = 10m, HasCoffee = true };

        await sut.RegisterAsync(coffeeEvent);

        await repository.Received(1).AddAsync(coffeeEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnRepositoryResult()
    {
        var repository = Substitute.For<ICoffeeEventRepository>();
        var expected = new List<CoffeeEvent> { new() { WeightInGrams = 8m, HasCoffee = true } };
        repository.GetRecentAsync(10, Arg.Any<CancellationToken>()).Returns(expected);
        var sut = new CoffeeEventService(repository);

        var result = await sut.GetRecentAsync(10);

        result.Should().BeEquivalentTo(expected);
    }
}
