using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class SafetyEvent
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public SafetyLevel Level { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public string? Region { get; set; }
    public bool Handled { get; set; }
}