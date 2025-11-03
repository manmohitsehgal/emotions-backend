using System.Collections.Concurrent;
using Emotions.Application.Interfaces;
using Emotions.Infrastructure.Options;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotions.Infrastructure.Services
{
    public sealed class AzureStreamingTranscriptionService : ILiveTranscriptionService, IAsyncDisposable
    {
        private readonly AzureSpeechOptions _opt;
        private readonly ILogger<AzureStreamingTranscriptionService> _log;

        private sealed class SessionState
        {
            public PushAudioInputStream? PushStream { get; set; }
            public AudioConfig? AudioConfig { get; set; }
            public SpeechRecognizer? Recognizer { get; set; }
            public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
            public int PendingChunks { get; set; } = 0;
        }

        private readonly ConcurrentDictionary<Guid, SessionState> _sessions = new();

        public event EventHandler<(Guid sessionId, string text, bool isFinal)>? OnCaption;

        public AzureStreamingTranscriptionService(IOptions<AzureSpeechOptions> opt,
            ILogger<AzureStreamingTranscriptionService> log)
        {
            _opt = opt.Value;
            _log = log;
        }

        public async Task StartAsync(Guid sessionId, string locale)
        {
            if (_sessions.ContainsKey(sessionId))
                return;

            if (_opt.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(_opt.SampleRate));
            if (_opt.Channels <= 0 || _opt.Channels > 2) throw new ArgumentOutOfRangeException(nameof(_opt.Channels));

            var speechConfig = SpeechConfig.FromSubscription(_opt.Key, _opt.Region);
            speechConfig.SpeechRecognitionLanguage = string.IsNullOrWhiteSpace(locale) ? _opt.DefaultLocale : locale;

            uint rate = (uint)_opt.SampleRate;
            byte bits = 16; // PCM16
            byte chans = (byte)_opt.Channels;

            var format = AudioStreamFormat.GetWaveFormatPCM(rate, bits, chans);
            var pushStream = AudioInputStream.CreatePushStream(format);
            var audioConfig = AudioConfig.FromStreamInput(pushStream);
            var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            recognizer.Recognizing += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                    OnCaption?.Invoke(this, (sessionId, e.Result.Text, false));
            };
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
                    OnCaption?.Invoke(this, (sessionId, e.Result.Text, true));
            };
            recognizer.Canceled += (s, e) =>
            {
                _log.LogWarning("Azure STT canceled: {Reason} {Error}", e.Reason, e.ErrorDetails);
            };

            var state = new SessionState
            {
                PushStream = pushStream,
                AudioConfig = audioConfig,
                Recognizer = recognizer,
                StartedAtUtc = DateTime.UtcNow
            };

            if (!_sessions.TryAdd(sessionId, state))
            {
                recognizer.Dispose();
                audioConfig.Dispose();
                pushStream.Dispose();
                throw new InvalidOperationException("Session already exists.");
            }

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            _log.LogInformation("Azure STT started for {SessionId}", sessionId);
        }

        public Task PushAsync(Guid sessionId, byte[] audio, int sampleRate, string encoding, int channels,
            int durationMs)
        {
            if (!_sessions.TryGetValue(sessionId, out var state) || state.PushStream is null)
                return Task.CompletedTask;

            if ((DateTime.UtcNow - state.StartedAtUtc).TotalSeconds > _opt.MaxSessionSeconds)
            {
                _log.LogWarning("Session {SessionId} exceeded MaxSessionSeconds; dropping audio", sessionId);
                return Task.CompletedTask;
            }

            if (sampleRate != _opt.SampleRate ||
                channels != _opt.Channels ||
                !string.Equals(encoding, _opt.Encoding, StringComparison.OrdinalIgnoreCase))
            {
                _log.LogWarning(
                    "Session {SessionId} mismatched format (sr={SampleRate}, ch={Channels}, enc={Encoding})",
                    sessionId, sampleRate, channels, encoding);
                return Task.CompletedTask;
            }

            if (state.PendingChunks > _opt.MaxPendingChunks)
            {
                _log.LogWarning("Session {SessionId} backpressure: pending={Pending}", sessionId, state.PendingChunks);
                return Task.CompletedTask;
            }

            try
            {
                state.PendingChunks++;
                state.PushStream!.Write(audio);
            }
            finally
            {
                state.PendingChunks--;
            }

            return Task.CompletedTask;
        }

        public async Task EndAsync(Guid sessionId)
        {
            if (!_sessions.TryRemove(sessionId, out var state))
                return;

            try
            {
                if (state.Recognizer is not null)
                    await state.Recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "StopContinuousRecognitionAsync error for {SessionId}", sessionId);
            }

            state.Recognizer?.Dispose();
            state.AudioConfig?.Dispose();
            state.PushStream?.Dispose();

            _log.LogInformation("Azure STT ended for {SessionId}", sessionId);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kv in _sessions)
                await EndAsync(kv.Key);
        }
    }
}