using System.ComponentModel.DataAnnotations;

namespace EventApi.Dtos;

public class EventRequest
{
    [Required]
    public string? Title { get; init; }

    public string? Description { get; init; }

    [Required]
    public DateTime? StartAt { get; init; }

    [Required]
    public DateTime? EndAt { get; init; }
}
