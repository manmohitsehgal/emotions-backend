using System.Security.Claims;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/journal-entries")]
[Authorize]
public class JournalEntriesController(AppDbContext db) : ControllerBase
{
    [HttpGet("{entryId:guid}/attachments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<JournalAttachment>>> ListAttachments(Guid entryId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var owned = await db.JournalEntries.AnyAsync(e => e.Id == entryId && e.UserId == userId);
        if (!owned) return NotFound();

        var atts = await db.Set<JournalAttachment>()
            .Where(a => a.EntryId == entryId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(atts);
    }
}