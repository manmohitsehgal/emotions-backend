using System.Collections.Concurrent;
using Emotions.Application.DTOs.Streaming;
using Emotions.Application.Interfaces;
using Emotions.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Emotions.Infrastructure.Services
{
    public sealed class DefaultLiveTherapyOrchestrator : ILiveTherapyOrchestrator
    {
        private readonly ILiveTranscriptionService _stt;
        private readonly IHubContext<VoiceHub> _hub;
        private readonly ConcurrentDictionary<Guid, string> _usersBySession = new();

        public DefaultLiveTherapyOrchestrator(ILiveTranscriptionService stt, IHubContext<VoiceHub> hub)
        {
            _stt = stt;
            _hub = hub;
            _stt.OnCaption += HandleCaption;
        }

        public async Task<Guid> StartAsync(string userId, StartSessionRequest req)
        {
            var sessionId = Guid.NewGuid();
            _usersBySession[sessionId] = userId;
            await _stt.StartAsync(sessionId, req.Locale ?? "en-US");
            return sessionId;
        }

        public Task PushAudioAsync(Guid sessionId, AudioChunkDto chunk)
            => _stt.PushAsync(sessionId, chunk.Data, chunk.SampleRate, chunk.Encoding, chunk.Channels,
                chunk.DurationMs);

        public async Task EndAsync(Guid sessionId, EndSessionRequest req)
        {
            await _stt.EndAsync(sessionId);
            if (!_usersBySession.TryGetValue(sessionId, out var userId)) return;

            if (req.GenerateReply)
            {
                var tokens = new[]
                {
                    "I hear you. ",
                    "That sounds tough. ",
                    "What felt most challenging ",
                    "about this for you?"
                };
                foreach (var t in tokens.Take(tokens.Length - 1))
                {
                    await _hub.Clients.User(userId).SendAsync("aiTokenDelta", new { text = t, isFinal = false });
                    await Task.Delay(120);
                }

                await _hub.Clients.User(userId).SendAsync("aiTokenDelta", new { text = tokens.Last(), isFinal = true });
            }

            _usersBySession.TryRemove(sessionId, out _);
        }

        private async void HandleCaption(object? sender, (Guid sessionId, string text, bool isFinal) e)
        {
            if (_usersBySession.TryGetValue(e.sessionId, out var userId))
            {
                await _hub.Clients.User(userId).SendAsync("captionDelta", new { text = e.text, isFinal = e.isFinal });
            }
        }
    }
}