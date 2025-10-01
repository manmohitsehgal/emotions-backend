namespace Emotions.Domain.Entities;

public class TranscriptSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttachmentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Model { get; set; } = "gpt-4o-mini";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}