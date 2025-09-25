namespace Emotions.Domain.Entities;

public class TranscriptSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttachmentId { get; set; }
    public int Index { get; set; } // order from provider
    public int StartMs { get; set; } // inclusive
    public int EndMs { get; set; } // exclusive
    public string Text { get; set; } = string.Empty;

    public List<TranscriptWord> Words { get; set; } = new();
}