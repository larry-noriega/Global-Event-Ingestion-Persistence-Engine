// src/Program.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using EventEngine.Api.Application.Services;
using EventEngine.Api.Domain.Interfaces;
using EventEngine.Api.Domain.State;
using EventEngine.Api.Infrastructure.Ingestion;
using EventEngine.Api.src.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ADD THIS LINE to kill the overhead of per-request logging
builder.Logging.SetMinimumLevel(LogLevel.Warning); 

// JSON Optimization with source generators
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Tells the Minimal API to use your source generator
    options.SerializerOptions.TypeInfoResolver = SystemTextJsonSourceGenerationContext.Default;
});

// Dependency Injection
builder.Services.AddSingleton<IStateEngine<EventState, Trigger>, EventStateEngine>();
builder.Services.AddScoped<IEventIngestor<string>, EventIngestor>(); // Assuming EventIngestor is implemented in Infrastructure layer
builder.Services.AddScoped<EventProcessingService<string>>();

var app = builder.Build();

app.MapPost("/ingest", async (string payload, IEventIngestor<string> eventIngestor, EventProcessingService<string> processingService) => 
{ 
    if (!eventIngestor.CapacityExceeded) 
    { 
        _ = processingService.ProcessEventAsync(payload, CancellationToken.None); 
        return Results.Accepted(uri: null); 
    } 
    
    return Results.Problem("capacity exceeded", statusCode: 503);
});


app.Run();

// Custom JSON Serializer Options for source generators
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(string))]
public partial class SystemTextJsonSourceGenerationContext : JsonSerializerContext
{

}