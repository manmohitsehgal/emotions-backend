using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class TherapyActionItems
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; } // Where it was proposed
    public string Title { get; set; } = default!;
    public string? Details { get; set; }
    public DateTime? DueAt { get; set; }
    public ActionStatus Status { get; set; } = ActionStatus.Open;
    public DateTime? CompletedAt { get; set; }
}