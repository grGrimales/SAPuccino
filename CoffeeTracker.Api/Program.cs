using CoffeeTracker.Api.Endpoints;
using CoffeeTracker.Api.Infrastructure.DependencyInjection;

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
