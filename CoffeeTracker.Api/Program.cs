using CoffeeTracker.Api.Application.State;
using CoffeeTracker.Api.Domain.Interfaces;
using CoffeeTracker.Api.Endpoints;
using CoffeeTracker.Api.Infrastructure.DependencyInjection;
using CoffeeTracker.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoffeeTrackerInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

const string FrontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// Inicialização melhor-esforço: aplica migrações e hidrata o estado do dia a partir do banco.
// Se o banco não estiver disponível, seguimos com o estado em memória (o tempo real continua funcionando).
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var tracker = app.Services.GetRequiredService<MachineStateTracker>();
        var repository = scope.ServiceProvider.GetRequiredService<ICoffeeEventRepository>();
        var (fromUtc, toUtc, localDate) = tracker.GetTodayWindow(DateTime.UtcNow);
        var coffeesToday = await repository.CountBetweenAsync(fromUtc, toUtc);
        var lastUsedUtc = await repository.GetLastOccurredAtUtcAsync();
        tracker.Seed(localDate, coffeesToday, lastUsedUtc);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not initialize state from database. Continuing with in-memory state.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.MapGet("/", () => Results.Ok(new { message = "Hola mundo desde el BE SAPuccino ☕", status = "ok" }))
    .WithName("HelloWorld");

app.MapCoffeeEndpoints();
app.MapHub<CoffeeTracker.Api.Hubs.CoffeeHub>("/hubs/coffee");

app.Run();

public partial class Program;
