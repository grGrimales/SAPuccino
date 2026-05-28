using CoffeeTracker.Api.Endpoints;
using CoffeeTracker.Api.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoffeeTrackerInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapCoffeeEndpoints();
app.MapHub<CoffeeTracker.Api.Hubs.CoffeeHub>("/hubs/coffee");

app.Run();

public partial class Program;
