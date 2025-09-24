namespace Emotions.Domain.Entities;

public class StreakCounter
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTime? LastEntryAt { get; set; }
}