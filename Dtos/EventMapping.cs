using EventApi.Models;

namespace EventApi.Dtos;

internal static class EventMapping
{
    public static Event ToEvent(this EventRequest request) => new()
    {
        Title = request.Title!,
        Description = request.Description,
        StartAt = request.StartAt!.Value.ToUniversalTime(),
        EndAt = request.EndAt!.Value.ToUniversalTime()
    };

    public static EventResponse ToResponse(this Event source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Description = source.Description,
        StartAt = source.StartAt,
        EndAt = source.EndAt
    };
}
