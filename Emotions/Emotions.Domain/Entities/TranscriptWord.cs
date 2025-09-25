namespace Emotions.Domain.Entities;

public class TranscriptWord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SegmentId { get; set; }
    public int Index { get; set; } // order inside segment
    public int StartMs { get; set; }
    public int EndMs { get; set; }
    public string Text { get; set; } = string.Empty;
}