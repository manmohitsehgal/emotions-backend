namespace Emotions.Domain.Entities;

public class JournalAttachment
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public JournalEntry Entry { get; set; } = null!;
    public string Type { get; set; } = "audio"; // audio|video|image
    public string BlobKey { get; set; } = string.Empty; // or Url
    public int? DurationSec { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}