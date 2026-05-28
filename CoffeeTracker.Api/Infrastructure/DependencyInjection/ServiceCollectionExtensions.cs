using CoffeeTracker.Api.Application.Services;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Infrastructure.Mqtt;
using CoffeeTracker.Api.Infrastructure.Persistence;
using CoffeeTracker.Api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTracker.Api.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoffeeTrackerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=coffee_tracker;Username=coffee_user;Password=coffee_pass";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ICoffeeEventRepository, CoffeeEventRepository>();
        services.AddScoped<ICoffeeEventService, CoffeeEventService>();
        services.AddSingleton<CoffeeDetector>();
        services.AddHostedService<MqttSubscriberService>();

        return services;
    }
}
