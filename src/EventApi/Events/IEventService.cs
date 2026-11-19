using EventApi.Events.Contracts;

namespace EventApi.Events;

public interface IEventService
{
    Task<EventResponse[]> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventWriteResult> CreateAsync(EventRequest request, CancellationToken cancellationToken = default);
    Task<EventWriteResult> UpdateAsync(Guid id, EventRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
