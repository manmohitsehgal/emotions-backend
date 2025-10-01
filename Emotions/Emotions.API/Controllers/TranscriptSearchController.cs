using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class TranscriptSearchController : ControllerBase
{
    private readonly AppDbContext _db;
    public TranscriptSearchController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Missing query");

        var term = $"%{q}%";

        var transcripts = _db.JournalAttachmentTranscripts
            .Where(t => t.Status == "Completed" && t.Text != null &&
                        (EF.Functions.ILike(t.Text!, term) || t.Text!.Contains(q)))
            .Select(t => new
            {
                t.AttachmentId, Source = "transcript", Snippet = t.Text!.Substring(0, Math.Min(240, t.Text!.Length))
            });

        var summaries = _db.TranscriptSummaries
            .Where(s => EF.Functions.ILike(s.Summary, term) || s.Summary.Contains(q) ||
                        (s.Tags != null && s.Tags.Any(tag => tag.Contains(q))))
            .Select(s => new
            {
                s.AttachmentId, Source = "summary", Snippet = s.Summary.Substring(0, Math.Min(240, s.Summary.Length))
            });

        var results = await transcripts.Union(summaries)
            .OrderBy(x => x.AttachmentId)
            .Take(50)
            .ToListAsync(ct);

        return Ok(results);
    }
}