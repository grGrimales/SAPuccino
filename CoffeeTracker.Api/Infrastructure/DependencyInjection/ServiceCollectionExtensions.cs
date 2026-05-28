using CoffeeTracker.Api.Application.Services;
using CoffeeTracker.Api.Application.State;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Infrastructure.Mqtt;
using CoffeeTracker.Api.Infrastructure.Persistence;
using CoffeeTracker.Api.Infrastructure.RealTime;
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

        var timeZoneId = configuration["App:TimeZone"] ?? "America/Sao_Paulo";
        services.AddSingleton(new MachineStateTracker(ResolveTimeZone(timeZoneId)));
        services.AddSingleton<CoffeeStatusNotifier>();

        services.AddHostedService<MqttSubscriberService>();

        return services;
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        // No Windows o id IANA pode não existir; tentamos variantes e caímos para UTC.
        foreach (var candidate in new[] { id, "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
