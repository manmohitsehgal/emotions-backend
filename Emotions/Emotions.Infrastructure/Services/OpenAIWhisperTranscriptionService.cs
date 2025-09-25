// Production transcription using OpenAI .NET SDK (Whisper-1)
// NuGet: OpenAI (official) >= 2.0

using Emotions.Application.Interfaces.Transcription;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;

namespace Emotions.Infrastructure.Services;

public sealed class OpenAIWhisperTranscriptionService : ITranscriptionService
{
    private readonly AppDbContext _db;
    private readonly IAttachmentReader _reader;
    private readonly ILogger<OpenAIWhisperTranscriptionService> _logger;
    private readonly AudioClient _audio;

    public OpenAIWhisperTranscriptionService(
        AppDbContext db,
        IAttachmentReader reader,
        ILogger<OpenAIWhisperTranscriptionService> logger,
        OpenAIClient openAiClient // registered in Program.cs
    )
    {
        _db = db;
        _reader = reader;
        _logger = logger;
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
                TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment
            };

            AudioTranscription result =
                await _audio.TranscribeAudioAsync(audio, audioFilename: $"{attachmentId}.m4a", options, ct);

            // 4) Persist
            tr.Status = "Completed";
            tr.Text = result.Text;
            tr.Language = result.Language;
            tr.Confidence = null; // Whisper doesn't return a single global confidence
            tr.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
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
}