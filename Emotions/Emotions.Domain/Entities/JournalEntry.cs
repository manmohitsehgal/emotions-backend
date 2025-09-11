namespace Emotions.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}