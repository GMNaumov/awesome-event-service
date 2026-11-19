using System.ComponentModel.DataAnnotations;
using EventApi.Dtos;
using EventApi.Errors;
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
builder.Services.AddSingleton<IProblemDetailsWriter, FallbackProblemDetailsWriter>();
builder.Services.AddValidation();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddSingleton<IEventService, InMemoryEventService>();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Event API v1");
    });
}

var events = app.MapGroup("/events")
    .ProducesProblem(StatusCodes.Status500InternalServerError);

events.MapGet("", async (IEventService service, CancellationToken cancellationToken) =>
{
    var storedEvents = await service.GetAllAsync(cancellationToken);
    return TypedResults.Ok(storedEvents.Select(EventMapping.ToResponse).ToArray());
});

events.MapGet("/{id:guid}", async Task<Results<Ok<EventResponse>, NotFound>> (
    Guid id, IEventService service, CancellationToken cancellationToken) =>
{
    var foundEvent = await service.GetByIdAsync(id, cancellationToken);
    return foundEvent is null ? TypedResults.NotFound() : TypedResults.Ok(foundEvent.ToResponse());
})
.WithName("GetEventById")
.ProducesProblem(StatusCodes.Status404NotFound);

events.MapPost("", async (EventRequest request, IEventService service, CancellationToken cancellationToken) =>
{
    var createdEvent = await service.CreateAsync(request.ToEvent(), cancellationToken);
    var response = createdEvent.ToResponse();
    return TypedResults.CreatedAtRoute(response, "GetEventById", new { id = response.Id });
})
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

events.MapPut("/{id:guid}", async Task<Results<Ok<EventResponse>, NotFound>> (
    Guid id, EventRequest request, IEventService service, CancellationToken cancellationToken) =>
{
    var storedEvent = await service.UpdateAsync(id, request.ToEvent(), cancellationToken);
    return storedEvent is null ? TypedResults.NotFound() : TypedResults.Ok(storedEvent.ToResponse());
})
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

events.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
    Guid id, IEventService service, CancellationToken cancellationToken) =>
{
    var deleted = await service.DeleteAsync(id, cancellationToken);
    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
})
.ProducesProblem(StatusCodes.Status404NotFound);

app.Run();
