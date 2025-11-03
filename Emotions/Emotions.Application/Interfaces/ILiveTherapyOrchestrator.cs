using Emotions.Application.DTOs.Streaming;

namespace Emotions.Application.Interfaces
{
    public interface ILiveTherapyOrchestrator
    {
        Task<Guid> StartAsync(string userId, StartSessionRequest req);
        Task PushAudioAsync(Guid sessionId, AudioChunkDto chunk);
        Task EndAsync(Guid sessionId, EndSessionRequest req);
    }
}