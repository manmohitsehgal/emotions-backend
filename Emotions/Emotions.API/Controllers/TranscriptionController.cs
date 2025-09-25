using Emotions.Application.DTOs;
using Emotions.Application.Interfaces.Transcription;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/transcription")]
[Authorize] // adjust policy
public sealed class TranscriptionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITranscriptionJobQueue _queue;

    public TranscriptionController(AppDbContext db, ITranscriptionJobQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    [HttpPost("{attachmentId:guid}/start")]
    public async Task<IActionResult> Start(Guid attachmentId, CancellationToken ct)
    {
        var existing = await _db.JournalAttachmentTranscripts
            .FirstOrDefaultAsync(t => t.AttachmentId == attachmentId, ct);
        if (existing is null)
        {
            _db.JournalAttachmentTranscripts.Add(new JournalAttachmentTranscripts
            {
                AttachmentId = attachmentId,
                Status = "Pending"
            });
            await _db.SaveChangesAsync(ct);
        }

        await _queue.QueueAsync(attachmentId);
        return Accepted(new { attachmentId, status = "Queued" });
    }

    [HttpGet("{attachmentId:guid}")]
    public async Task<ActionResult<TranscriptResponse>> Get(Guid attachmentId, CancellationToken ct)
    {
        var tr = await _db.JournalAttachmentTranscripts
            .Where(t => t.AttachmentId == attachmentId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (tr is null) return NotFound();

        return new TranscriptResponse(tr.AttachmentId, tr.Status, tr.Language, tr.Text, tr.Confidence);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(Guid attachmentId, CancellationToken ct)
    {
        var rows = await _db.JournalAttachmentTranscripts
            .Where(t => t.AttachmentId == attachmentId).ToListAsync(ct);
        if (rows.Count == 0) return NotFound();
        _db.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}