namespace Emotions.Domain.Entities
{
    public class ReliefSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Type { get; set; } = "physiological_sigh";
        public int DurationSec { get; set; }
        public int? MoodBefore { get; set; }
        public int? MoodAfter { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}