using System.ComponentModel.DataAnnotations;
using EventApi.Events.Contracts;

namespace EventApi.Events;

public class InMemoryEventService : IEventService
{
    private readonly Dictionary<Guid, Event> events = new();
    private readonly Lock sync = new();

    public Task<EventResponse[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            return Task.FromResult(events.Values.Select(ToResponse).ToArray());
        }
    }

    public Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            return Task.FromResult(events.TryGetValue(id, out var storedEvent) ? ToResponse(storedEvent) : null);
        }
    }

    public Task<EventWriteResult> CreateAsync(EventRequest request, CancellationToken cancellationToken = default)
        => SaveAsync(null, request, cancellationToken);

    public Task<EventWriteResult> UpdateAsync(
        Guid id, EventRequest request, CancellationToken cancellationToken = default)
        => SaveAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            return Task.FromResult(events.Remove(id));
        }
    }

    private Task<EventWriteResult> SaveAsync(Guid? id, EventRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Task.FromResult<EventWriteResult>(new EventWriteResult.Invalid(errors));
        }

        var storedEvent = new Event
        {
            Id = id ?? Guid.NewGuid(),
            Title = request.Title!,
            Description = request.Description,
            StartAt = request.StartAt!.Value.ToUniversalTime(),
            EndAt = request.EndAt!.Value.ToUniversalTime()
        };

        lock (sync)
        {
            if (id is null)
            {
                events.Add(storedEvent.Id, storedEvent);
            }
            else if (events.ContainsKey(storedEvent.Id))
            {
                events[storedEvent.Id] = storedEvent;
            }
            else
            {
                return Task.FromResult<EventWriteResult>(new EventWriteResult.NotFound());
            }
        }

        return Task.FromResult<EventWriteResult>(new EventWriteResult.Saved(ToResponse(storedEvent)));
    }

    private static Dictionary<string, string[]> Validate(EventRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        var errors = validationResults
            .SelectMany(result => result.MemberNames.Select(name => (Name: name, result.ErrorMessage)))
            .GroupBy(error => error.Name)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage!).ToArray());

        if (request.StartAt is { Kind: DateTimeKind.Unspecified })
        {
            errors[nameof(request.StartAt)] = ["Дата начала должна содержать информацию о часовом поясе."];
        }

        if (request.EndAt is { Kind: DateTimeKind.Unspecified })
        {
            errors[nameof(request.EndAt)] = ["Дата окончания должна содержать информацию о часовом поясе."];
        }

        if (request.StartAt is { Kind: not DateTimeKind.Unspecified } startAt &&
            request.EndAt is { Kind: not DateTimeKind.Unspecified } endAt &&
            endAt.ToUniversalTime() <= startAt.ToUniversalTime())
        {
            errors[nameof(request.EndAt)] = ["Дата окончания должна быть позже даты начала."];
        }

        return errors;
    }

    private static EventResponse ToResponse(Event source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Description = source.Description,
        StartAt = source.StartAt,
        EndAt = source.EndAt
    };
}
