using EventApi.Dtos;

namespace EventApi.Services;

public abstract record EventWriteResult
{
    private EventWriteResult()
    {
    }

    public sealed record Saved(EventResponse Response) : EventWriteResult;
    public sealed record Invalid(Dictionary<string, string[]> Errors) : EventWriteResult;
    public sealed record NotFound : EventWriteResult;
}
