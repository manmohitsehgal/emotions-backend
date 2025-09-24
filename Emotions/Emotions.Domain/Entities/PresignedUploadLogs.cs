namespace Emotions.Domain.Entities;

public class PresignedUploadLogs
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public string BlobKey { get; set; } = string.Empty; // pending key
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}