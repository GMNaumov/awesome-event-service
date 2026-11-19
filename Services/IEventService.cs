using EventApi.Models;

namespace EventApi.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event newEvent, CancellationToken cancellationToken = default);
}
