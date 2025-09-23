namespace Emotions.Domain.Entities;

public class DeepDiveSession
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public int StepIndex { get; set; }
    public string StepKey { get; set; } = null!; // e.g., identify_thought
    public string DataJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}