using System.Diagnostics;
using EventApi.Dtos;
using EventApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EventApi.Endpoints;

internal static class EventEndpoints
{
    private const string GetEventByIdRoute = "GetEventById";

    public static void MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var events = endpoints.MapGroup("/events")
            .WithTags("EventApi")
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        events.MapGet("", GetAllAsync);

        events.MapGet("/{id:guid}", GetByIdAsync)
            .WithName(GetEventByIdRoute)
            .ProducesProblem(StatusCodes.Status404NotFound);

        events.MapPost("", CreateAsync)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        events.MapPut("/{id:guid}", UpdateAsync)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        events.MapDelete("/{id:guid}", DeleteAsync)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<EventResponse[]>> GetAllAsync(
        IEventService service, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.GetAllAsync(cancellationToken));
    }

    private static async Task<Results<Ok<EventResponse>, NotFound>> GetByIdAsync(
        Guid id, IEventService service, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }

    private static async Task<Results<CreatedAtRoute<EventResponse>, ValidationProblem>> CreateAsync(
        EventRequest request, IEventService service, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result switch
        {
            EventWriteResult.Saved(var response) =>
                TypedResults.CreatedAtRoute(response, GetEventByIdRoute, new { id = response.Id }),
            EventWriteResult.Invalid(var errors) => ToValidationProblem(errors, context),
            _ => throw new InvalidOperationException("Unexpected result when creating an event.")
        };
    }

    private static async Task<Results<Ok<EventResponse>, NotFound, ValidationProblem>> UpdateAsync(
        Guid id, EventRequest request, IEventService service, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result switch
        {
            EventWriteResult.Saved(var response) => TypedResults.Ok(response),
            EventWriteResult.Invalid(var errors) => ToValidationProblem(errors, context),
            EventWriteResult.NotFound => TypedResults.NotFound(),
            _ => throw new InvalidOperationException("Unexpected result when updating an event.")
        };
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id, IEventService service, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static ValidationProblem ToValidationProblem(Dictionary<string, string[]> errors, HttpContext context)
    {
        return TypedResults.ValidationProblem(errors, extensions: new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
        });
    }
}
