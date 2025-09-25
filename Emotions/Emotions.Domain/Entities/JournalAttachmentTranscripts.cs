namespace Emotions.Domain.Entities;

public class JournalAttachmentTranscripts
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttachmentId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending|Processing|Completed|Failed
    public string? Language { get; set; } // e.g., "en"
    public string? Text { get; set; }
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}