namespace Emotions.Domain.Entities;

public class JournalAttachment
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }

    public JournalEntry Entry { get; set; } = null!;

// "audio" | "video" | "image"
    public string Type { get; set; } = "audio";

// Opaque key in storage
    public string BlobKey { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long SizeBytes { get; set; }
    public int? DurationSec { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}