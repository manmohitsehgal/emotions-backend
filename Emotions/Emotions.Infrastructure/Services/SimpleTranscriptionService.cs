using System.Collections.Concurrent;
using System.Text;
using Emotions.Application.Interfaces;

namespace Emotions.Infrastructure.Services
{
    public sealed class SimpleTranscriptionService : ILiveTranscriptionService
    {
        private readonly ConcurrentDictionary<Guid, StringBuilder> _buffers = new();

        public event EventHandler<(Guid sessionId, string text, bool isFinal)>? OnCaption;

        public Task StartAsync(Guid sessionId, string locale)
        {
            _buffers[sessionId] = new StringBuilder();
            return Task.CompletedTask;
        }

        public Task PushAsync(Guid sessionId, byte[] audio, int sampleRate, string encoding, int channels,
            int durationMs)
        {
            if (_buffers.TryGetValue(sessionId, out var sb))
            {
                sb.Append("[…]");
                OnCaption?.Invoke(this, (sessionId, "Listening…", false));
            }

            return Task.CompletedTask;
        }

        public Task EndAsync(Guid sessionId)
        {
            if (_buffers.TryGetValue(sessionId, out var sb))
            {
                var text = sb.ToString().Length == 0
                    ? "(no speech detected)"
                    : "Thanks for sharing. Tell me more about that.";
                OnCaption?.Invoke(this, (sessionId, text, true));
            }

            _buffers.TryRemove(sessionId, out _);
            return Task.CompletedTask;
        }
    }
}