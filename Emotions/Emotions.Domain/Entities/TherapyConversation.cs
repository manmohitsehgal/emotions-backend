using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class TherapyConversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TherapyMode Mode { get; set; } = TherapyMode.Vent;
    public bool IncludeJournal { get; set; }
    public bool IsPremiumSnapshot { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public SafetyLevel SafetyLevel { get; set; } = SafetyLevel.Normal;
}