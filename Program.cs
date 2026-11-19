using System.ComponentModel.DataAnnotations;
using EventApi.Models;
using EventApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, context, _) =>
{
    if (context.JsonPropertyInfo?.AttributeProvider?.IsDefined(typeof(RequiredAttribute), inherit: true) == true)
    {
        schema.Type &= ~JsonSchemaType.Null;
    }

    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton<IEventService, InMemoryEventService>();

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Event API v1");
    });
}

var events = app.MapGroup("/events");

events.MapGet("", async (IEventService service, CancellationToken cancellationToken) =>
{
    return TypedResults.Ok(await service.GetAllAsync(cancellationToken));
});

events.MapGet("/{id:guid}", async Task<Results<Ok<Event>, NotFound>> (
    Guid id, IEventService service, CancellationToken cancellationToken) =>
{
    var foundEvent = await service.GetByIdAsync(id, cancellationToken);
    return foundEvent is null ? TypedResults.NotFound() : TypedResults.Ok(foundEvent);
})
.WithName("GetEventById");

events.MapPost("", async (EventRequest request, IEventService service, CancellationToken cancellationToken) =>
{
    var createdEvent = await service.CreateAsync(request.ToEvent(), cancellationToken);
    return TypedResults.CreatedAtRoute(createdEvent, "GetEventById", new { id = createdEvent.Id });
})
.ProducesValidationProblem();

events.MapPut("/{id:guid}", async Task<Results<Ok<Event>, NotFound>> (
    Guid id, EventRequest request, IEventService service, CancellationToken cancellationToken) =>
{
    var storedEvent = await service.UpdateAsync(id, request.ToEvent(), cancellationToken);
    return storedEvent is null ? TypedResults.NotFound() : TypedResults.Ok(storedEvent);
})
.ProducesValidationProblem();

events.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
    Guid id, IEventService service, CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteAsync(id, cancellationToken);
    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
