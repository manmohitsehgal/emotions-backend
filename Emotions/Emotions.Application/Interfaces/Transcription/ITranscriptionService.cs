namespace Emotions.Application.Interfaces.Transcription;

public interface ITranscriptionService
{
    Task StartTranscriptionAsync(Guid attachmentId, CancellationToken ct = default);
}