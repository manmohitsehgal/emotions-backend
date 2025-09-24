namespace Emotions.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string BodyCipher { get; set; } = string.Empty;
    public string Mode { get; set; } = "text"; // text|audio|video|hybrid
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Mood { get; set; }
    public string Privacy { get; set; } = "AIEnhanced";
    public bool IsArchived { get; set; }
    public List<JournalAttachment> Attachments { get; set; } = new();
}