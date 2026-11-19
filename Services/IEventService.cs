using EventApi.Models;

namespace EventApi.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event newEvent, CancellationToken cancellationToken = default);
    Task<Event?> UpdateAsync(Guid id, Event updatedEvent, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
