using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class SessionTemplate
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public SpeakPolicy SpeakPolicy { get; set; } = SpeakPolicy.RoundRobin;
    public int DefaultCapacity { get; set; } = 15;
    public bool AllowListeners { get; set; } = true;


// Optional: Recurrence metadata for a scheduler service
    public string? RecurrenceRule { get; set; } // e.g., iCal RRULE string
}