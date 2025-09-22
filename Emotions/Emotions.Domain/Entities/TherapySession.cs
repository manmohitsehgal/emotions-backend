using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class TherapySession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TherapistId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public TherapySessionStatus Status { get; set; } = TherapySessionStatus.Scheduled;
}