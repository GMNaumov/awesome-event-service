using EventApi.Models;

namespace EventApi.Services;

public class InMemoryEventService : IEventService
{
    private readonly Dictionary<Guid, Event> events = new();
    private readonly Lock sync = new();

    public Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            return Task.FromResult<IEnumerable<Event>>(events.Values.Select(Copy).ToArray());
        }
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (sync)
        {
            return Task.FromResult<Event?>(events.TryGetValue(id, out var storedEvent) ? Copy(storedEvent) : null);
        }
    }

    public Task<Event> CreateAsync(Event newEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(newEvent);

        var storedEvent = Copy(newEvent);
        storedEvent.Id = Guid.NewGuid();

        lock (sync)
        {
            events.Add(storedEvent.Id, storedEvent);
        }

        return Task.FromResult(Copy(storedEvent));
    }

    private static Event Copy(Event source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Description = source.Description,
        StartAt = source.StartAt,
        EndAt = source.EndAt
    };
}
