using System.ComponentModel.DataAnnotations;

namespace EventApi.Models;

public class EventRequest : IValidatableObject
{
    [Required]
    public string? Title { get; init; }

    public string? Description { get; init; }

    [Required]
    public DateTime? StartAt { get; init; }

    [Required]
    public DateTime? EndAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt is DateTime startAt && EndAt is DateTime endAt &&
            endAt.ToUniversalTime() <= startAt.ToUniversalTime())
        {
            yield return new ValidationResult("EndAt must be later than StartAt.", [nameof(EndAt)]);
        }
    }

    public Event ToEvent() => new()
    {
        Title = Title!,
        Description = Description,
        StartAt = StartAt!.Value,
        EndAt = EndAt!.Value
    };
}
