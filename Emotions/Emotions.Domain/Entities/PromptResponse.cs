namespace Emotions.Domain.Entities
{
    public class PromptResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Kind { get; set; } = "headline|nudge";
        public string PromptId { get; set; } = "";
        public string Text { get; set; } = "";
        public string? MoodTag { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}