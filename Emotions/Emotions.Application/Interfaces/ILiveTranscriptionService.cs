namespace Emotions.Application.Interfaces
{
    public interface ILiveTranscriptionService
    {
        Task StartAsync(Guid sessionId, string locale);
        Task PushAsync(Guid sessionId, byte[] audio, int sampleRate, string encoding, int channels, int durationMs);
        Task EndAsync(Guid sessionId);

        event EventHandler<(Guid sessionId, string text, bool isFinal)>? OnCaption;
    }
}