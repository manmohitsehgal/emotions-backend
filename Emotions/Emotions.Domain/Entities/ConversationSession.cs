namespace Emotions.Domain.Entities
{
    public class ConversationSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public string Mode { get; set; } = "talk";
        public string Locale { get; set; } = "en-US";
    }
}