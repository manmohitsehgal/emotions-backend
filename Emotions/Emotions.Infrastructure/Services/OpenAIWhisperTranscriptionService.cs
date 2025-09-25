using System.Text.RegularExpressions;
using Emotions.Application.Interfaces.Transcription;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;

namespace Emotions.Infrastructure.Services
{
    /// <summary>
    /// Production Whisper transcription:
    /// - Writes/updates JournalAttachmentTranscript row
    /// - Calls OpenAI Whisper-1 with verbose timestamps
    /// - Persists segments/words (Sprint 6) and supports optional PII redaction
    /// </summary>
    public sealed class OpenAIWhisperTranscriptionService : ITranscriptionService
    {
        private readonly AppDbContext _db;
        private readonly IAttachmentReader _reader;
        private readonly ILogger<OpenAIWhisperTranscriptionService> _logger;
        private readonly AudioClient _audio;
        private readonly IConfiguration _config;

        public OpenAIWhisperTranscriptionService(
            AppDbContext db,
            IAttachmentReader reader,
            ILogger<OpenAIWhisperTranscriptionService> logger,
            OpenAIClient openAiClient,
            IConfiguration config // used for Transcription:RedactPII
        )
        {
            _db = db;
            _reader = reader;
            _logger = logger;
            _config = config;
            _audio = openAiClient.GetAudioClient("whisper-1");
        }

        public async Task StartTranscriptionAsync(Guid attachmentId, CancellationToken ct = default)
        {
            // 1) Locate attachment & ensure transcript row exists
            var attachment = await _db.Set<JournalAttachment>()
                                 .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
                             ?? throw new InvalidOperationException($"Attachment {attachmentId} not found.");

            var tr = await _db.JournalAttachmentTranscripts
                .FirstOrDefaultAsync(t => t.AttachmentId == attachmentId, ct);

            if (tr is null)
            {
                tr = new JournalAttachmentTranscripts
                {
                    AttachmentId = attachmentId,
                    Status = "Processing",
                    CreatedAt = DateTime.UtcNow
                };
                _db.Add(tr);
            }
            else
            {
                tr.Status = "Processing";
                tr.CompletedAt = null;
            }

            await _db.SaveChangesAsync(ct);

            try
            {
                // 2) Read audio stream from storage
                await using Stream audio = await _reader.OpenReadAsync(attachment.BlobKey, ct);

                // 3) Transcribe with word+segment timestamps
                var options = new AudioTranscriptionOptions
                {
                    ResponseFormat = AudioTranscriptionFormat.Verbose,
                    TimestampGranularities =
                        AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment
                };

                AudioTranscription result = await _audio.TranscribeAudioAsync(
                    audio,
                    audioFilename: $"{attachmentId}.m4a",
                    options,
                    ct
                );

                // 4) Persist transcript (summary fields)
                tr.Status = "Completed";
                tr.Text = result.Text;
                tr.Language = result.Language;
                tr.Confidence = null; // Whisper doesn't return a single global confidence
                tr.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                // 5) Persist timings (segments + words)
                await PersistTimingsAsync(attachmentId, result, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcription failed for {AttachmentId}", attachmentId);
                tr.Status = "Failed";
                await _db.SaveChangesAsync(ct);
                throw;
            }
        }

        // ---------- Sprint 6 helpers ----------

        private static string MaybeRedact(string text, bool redact)
        {
            if (!redact) return text;
            // Simple PII redactions: emails, phone-like numbers
            var t = Regex.Replace(text, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "[redacted-email]");
            t = Regex.Replace(t, @"\b(?:\+?\d[\s-]?){7,}\b", "[redacted-phone]");
            return t;
        }

        private async Task PersistTimingsAsync(Guid attachmentId, AudioTranscription result, CancellationToken ct)
        {
            // 0) Idempotency: wipe old
            var oldSegs = await _db.TranscriptSegments
                .Where(s => s.AttachmentId == attachmentId)
                .ToListAsync(ct);

            if (oldSegs.Count > 0)
            {
                var oldSegIds = oldSegs.Select(s => s.Id).ToList();
                var oldWords = _db.TranscriptWords.Where(w => oldSegIds.Contains(w.SegmentId));
                _db.RemoveRange(oldWords);
                _db.RemoveRange(oldSegs);
                await _db.SaveChangesAsync(ct);
            }

            bool redact = _config.GetValue("Transcription:RedactPII", false);

            // 1) Segments (IReadOnlyList<TranscribedSegment>)
            var segs = result.Segments ?? Array.Empty<TranscribedSegment>();
            var newSegs = new List<TranscriptSegment>(segs.Count);

            for (int i = 0; i < segs.Count; i++)
            {
                var s = segs[i];
                var seg = new TranscriptSegment
                {
                    AttachmentId = attachmentId,
                    Index = i,
                    StartMs = (int)Math.Round(s.StartTime.TotalMilliseconds),
                    EndMs = (int)Math.Round(s.EndTime.TotalMilliseconds),
                    Text = MaybeRedact(s.Text ?? string.Empty, redact),
                };
                newSegs.Add(seg);
            }

            if (newSegs.Count > 0)
            {
                _db.AddRange(newSegs);
                await _db.SaveChangesAsync(ct);
            }

            // 2) Words (flat list: IReadOnlyList<TranscribedWord>)
            var words = result.Words ?? Array.Empty<TranscribedWord>();
            if (newSegs.Count == 0 || words.Count == 0)
                return;

            // two-pointer sweep to map each word to its owning segment window
            int si = 0; // segment index
            int inserted = 0;

            foreach (var w in words)
            {
                var wStart = w.StartTime;
                // advance to the segment whose EndTime is after the word start
                while (si < segs.Count && wStart >= segs[si].EndTime) si++;
                if (si >= segs.Count) break; // remaining words are beyond last segment

                // skip stray words that begin before first segment starts
                if (wStart < segs[si].StartTime) continue;

                _db.TranscriptWords.Add(new TranscriptWord
                {
                    SegmentId = newSegs[si].Id,
                    Index = inserted, // global order; if you prefer per-segment, track per-seg counters
                    StartMs = (int)Math.Round(w.StartTime.TotalMilliseconds),
                    EndMs = (int)Math.Round(w.EndTime.TotalMilliseconds),
                    Text = MaybeRedact(w.Word ?? string.Empty, redact),
                });
                inserted++;
            }

            if (inserted > 0)
                await _db.SaveChangesAsync(ct);
        }
    }
}