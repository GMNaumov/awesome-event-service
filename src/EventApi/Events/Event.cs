namespace EventApi.Events;

public class Event
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}
