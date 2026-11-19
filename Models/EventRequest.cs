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
        if (StartAt?.Kind == DateTimeKind.Unspecified)
        {
            yield return new ValidationResult("StartAt must include Z or a UTC offset.", [nameof(StartAt)]);
        }

        if (EndAt?.Kind == DateTimeKind.Unspecified)
        {
            yield return new ValidationResult("EndAt must include Z or a UTC offset.", [nameof(EndAt)]);
        }

        if (StartAt is { Kind: not DateTimeKind.Unspecified } startAt &&
            EndAt is { Kind: not DateTimeKind.Unspecified } endAt &&
            endAt.ToUniversalTime() <= startAt.ToUniversalTime())
        {
            yield return new ValidationResult("EndAt must be later than StartAt.", [nameof(EndAt)]);
        }
    }

    public Event ToEvent() => new()
    {
        Title = Title!,
        Description = Description,
        StartAt = StartAt!.Value.ToUniversalTime(),
        EndAt = EndAt!.Value.ToUniversalTime()
    };
}
