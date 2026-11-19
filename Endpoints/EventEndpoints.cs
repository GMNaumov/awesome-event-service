using System.Text.Json;
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

    private static async Task<Results<Ok<EventResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id, IEventService service, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response is null ? EventNotFound() : TypedResults.Ok(response);
    }

    private static async Task<Results<CreatedAtRoute<EventResponse>, ValidationProblem>> CreateAsync(
        EventRequest request, IEventService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result switch
        {
            EventWriteResult.Saved(var response) =>
                TypedResults.CreatedAtRoute(response, GetEventByIdRoute, new { id = response.Id }),
            EventWriteResult.Invalid(var errors) => ToValidationProblem(errors),
            _ => throw new InvalidOperationException("Unexpected result when creating an event.")
        };
    }

    private static async Task<Results<Ok<EventResponse>, ProblemHttpResult, ValidationProblem>> UpdateAsync(
        Guid id, EventRequest request, IEventService service, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result switch
        {
            EventWriteResult.Saved(var response) => TypedResults.Ok(response),
            EventWriteResult.Invalid(var errors) => ToValidationProblem(errors),
            EventWriteResult.NotFound => EventNotFound(),
            _ => throw new InvalidOperationException("Unexpected result when updating an event.")
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id, IEventService service, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : EventNotFound();
    }

    private static ProblemHttpResult EventNotFound()
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            detail: "No event exists with the specified id.");
    }

    private static ValidationProblem ToValidationProblem(Dictionary<string, string[]> errors)
    {
        return TypedResults.ValidationProblem(errors.ToDictionary(
            error => JsonNamingPolicy.CamelCase.ConvertName(error.Key),
            error => error.Value));
    }
}
