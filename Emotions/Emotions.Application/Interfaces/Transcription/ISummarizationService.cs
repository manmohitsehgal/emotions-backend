namespace Emotions.Application.Interfaces.Transcription;

public interface ISummarizationService
{
    Task SummarizeAttachmentAsync(Guid attachmentId, CancellationToken ct = default);
}