using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class SessionBooking
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public SupportSession Session { get; set; } = default!;


    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; } // when user actually joins


// Priority rules
    public bool IsPremium { get; set; }
    public int PriorityScore { get; set; } // derived (e.g., premium + returner)
}