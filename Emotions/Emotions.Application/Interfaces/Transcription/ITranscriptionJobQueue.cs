namespace Emotions.Application.Interfaces.Transcription;

public interface ITranscriptionJobQueue
{
    ValueTask QueueAsync(Guid attachmentId);
    ValueTask<Guid?> DequeueAsync(CancellationToken ct);
}