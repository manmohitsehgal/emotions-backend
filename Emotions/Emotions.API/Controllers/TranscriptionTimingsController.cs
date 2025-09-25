using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/transcription")]
[Authorize]
public sealed class TranscriptionTimingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TranscriptionTimingsController(AppDbContext db) => _db = db;

    [HttpGet("{attachmentId:guid}/segments")]
    public async Task<IActionResult> GetSegments(Guid attachmentId, CancellationToken ct)
    {
        var items = await _db.TranscriptSegments
            .Where(s => s.AttachmentId == attachmentId)
            .OrderBy(s => s.Index)
            .Select(s => new { s.Id, s.Index, s.StartMs, s.EndMs, s.Text })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("segments/{segmentId:guid}/words")]
    public async Task<IActionResult> GetWords(Guid segmentId, CancellationToken ct)
    {
        var items = await _db.TranscriptWords
            .Where(w => w.SegmentId == segmentId)
            .OrderBy(w => w.Index)
            .Select(w => new { w.Id, w.Index, w.StartMs, w.EndMs, w.Text })
            .ToListAsync(ct);
        return Ok(items);
    }

    // Convenience combined payload if the list isn't huge
    [HttpGet("{attachmentId:guid}/timings")]
    public async Task<IActionResult> GetTimings(Guid attachmentId, CancellationToken ct)
    {
        var segs = await _db.TranscriptSegments
            .Where(s => s.AttachmentId == attachmentId)
            .OrderBy(s => s.Index)
            .Select(s => new { s.Id, s.Index, s.StartMs, s.EndMs, s.Text })
            .ToListAsync(ct);

        var segIds = segs.Select(s => s.Id).ToList();
        var words = await _db.TranscriptWords
            .Where(w => segIds.Contains(w.SegmentId))
            .OrderBy(w => w.Index)
            .Select(w => new { w.SegmentId, w.Index, w.StartMs, w.EndMs, w.Text })
            .ToListAsync(ct);

        var result = segs.Select(s => new
        {
            s.Id,
            s.Index,
            s.StartMs,
            s.EndMs,
            s.Text,
            words = words.Where(w => w.SegmentId == s.Id).ToList()
        });

        return Ok(result);
    }
}