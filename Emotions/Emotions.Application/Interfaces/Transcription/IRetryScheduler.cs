namespace Emotions.Application.Interfaces.Transcription;

public interface IRetryScheduler
{
    ValueTask ScheduleRetryAsync(Guid attachmentId, TimeSpan delay, CancellationToken ct = default);
}