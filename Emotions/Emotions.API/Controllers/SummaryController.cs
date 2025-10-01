using Emotions.Application.Interfaces.Transcription;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/summary")]
[Authorize]
public sealed class SummaryController : ControllerBase
{
    private readonly ISummarizationService _service;
    private readonly AppDbContext _db;

    public SummaryController(ISummarizationService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpPost("{attachmentId:guid}/start")]
    public async Task<IActionResult> Start(Guid attachmentId, CancellationToken ct)
    {
        await _service.SummarizeAttachmentAsync(attachmentId, ct);
        return Accepted(new { attachmentId, status = "Completed" });
    }

    [HttpGet("{attachmentId:guid}")]
    public async Task<IActionResult> Get(Guid attachmentId, CancellationToken ct)
    {
        var s = await _db.TranscriptSummaries
            .Where(x => x.AttachmentId == attachmentId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.AttachmentId, x.Summary, x.Tags, x.Model, x.CreatedAt })
            .FirstOrDefaultAsync(ct);

        if (s is null) return NotFound();
        return Ok(s);
    }
}