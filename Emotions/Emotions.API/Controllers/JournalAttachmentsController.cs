using System.Security.Claims;
using Emotions.API.Policies;
using Emotions.Application.DTOs;
using Emotions.Application.Storage;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/journal-attachments")]
[Authorize]
public class JournalAttachmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorage _storage;
    private readonly ILogger<JournalAttachmentsController> _log;


    public JournalAttachmentsController(AppDbContext db, IBlobStorage storage,
        ILogger<JournalAttachmentsController> log)
    {
        _db = db;
        _storage = storage;
        _log = log;
    }

    [HttpGet("{attachmentId:guid}/read-url")]
    public async Task<ActionResult<ReadUrlResponse>> GetReadUrl(Guid attachmentId, [FromQuery] int minutes = 10)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var att = await _db.Set<JournalAttachment>()
            .Include(a => a.Entry)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Entry.UserId == userId);

        if (att is null) return NotFound();

        var ttl = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 60));
        var url = _storage.GetReadSasUrl(att.BlobKey, ttl);
        return Ok(new ReadUrlResponse(url, DateTimeOffset.UtcNow.Add(ttl)));
    }

    [HttpGet("{attachmentId:guid}/preview-url")]
    public async Task<ActionResult<ReadUrlResponse>> GetPreviewUrl(Guid attachmentId, [FromQuery] int minutes = 10)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var att = await _db.Set<JournalAttachment>()
            .Include(a => a.Entry)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Entry.UserId == userId);

        if (att?.PreviewBlobKey is null) return NotFound();

        var ttl = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 60));
        var url = _storage.GetReadSasUrl(att.PreviewBlobKey, ttl);
        return Ok(new ReadUrlResponse(url, DateTimeOffset.UtcNow.Add(ttl)));
    }

    [HttpPost("presign")]
    public async Task<ActionResult<PresignResponse>> Presign([FromBody] PresignRequest req)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized("Invalid user id claim.");

        var entry = await _db.JournalEntries
            .Where(e => e.Id == req.EntryId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (entry is null) return NotFound("Entry not found");


        if (req.SizeBytes <= 0 || req.SizeBytes > UploadPolicy.MaxFor(req.Type))
            return BadRequest("Size exceeds limit");
        if (!UploadPolicy.IsAllowed(req.Type, req.MimeType)) return BadRequest("Unsupported MIME type");


        var ext = Path.GetExtension(req.FileName ?? "");
        var pendingKey = $"entries/{entry.Id}/_pending/{Guid.NewGuid():N}{ext}";


        var pre = await _storage.CreatePresignedUploadAsync(pendingKey, req.MimeType, req.SizeBytes,
            req.ContentMD5Base64);


// optional: log presign for reaper
        _db.Add(new PresignedUploadLogs { Id = Guid.NewGuid(), EntryId = entry.Id, BlobKey = pre.BlobKey });
        await _db.SaveChangesAsync();


        _log.LogInformation("Presigned upload for {EntryId} by {User} → {BlobKey} ({Mime}, {Size}B)", entry.Id, userId,
            pre.BlobKey, req.MimeType, req.SizeBytes);
        return Ok(new PresignResponse(pre.BlobKey, pre.UploadUrl, pre.Method, (Dictionary<string, string>)pre.Headers));
    }

    public record SetPreviewRequest(string BlobKey);

    [HttpPost("{attachmentId:guid}/set-preview")]
    public async Task<IActionResult> SetPreview(Guid attachmentId, [FromBody] SetPreviewRequest body)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var att = await _db.Set<JournalAttachment>()
            .Include(a => a.Entry)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Entry.UserId == userId);
        if (att is null) return NotFound();

        // (Optional) verify blob exists
        if (!await _storage.BlobExistsAsync(body.BlobKey)) return BadRequest("Preview blob not found");

        att.PreviewBlobKey = body.BlobKey;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record SetWaveformRequest(string WaveformJson, int? DurationSec);

    [HttpPost("{attachmentId:guid}/set-waveform")]
    public async Task<IActionResult> SetWaveform(Guid attachmentId, [FromBody] SetWaveformRequest body)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var att = await _db.Set<JournalAttachment>()
            .Include(a => a.Entry)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Entry.UserId == userId && a.Type == "audio");
        if (att is null) return NotFound();

        att.WaveformJson = body.WaveformJson;
        if (body.DurationSec.HasValue) att.DurationSec = body.DurationSec;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("commit")]
    public async Task<ActionResult<JournalAttachment>> Commit([FromBody] CommitRequest req)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized("Invalid user id claim.");

        var entry = await _db.JournalEntries
            .Where(e => e.Id == req.EntryId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (entry is null) return NotFound("Entry not found");


        var existing = await _db.Set<JournalAttachment>()
            .FirstOrDefaultAsync(a => a.EntryId == req.EntryId && a.BlobKey == req.BlobKey);
        if (existing is not null) return Ok(existing);


        if (!req.BlobKey.Contains("/_pending/")) return BadRequest("Invalid blob state");
        var exists = await _storage.BlobExistsAsync(req.BlobKey);
        if (!exists) return BadRequest("Blob not found");


        var props = await _storage.GetPropertiesAsync(req.BlobKey);
        if (props is null) return BadRequest("Blob not found");
        if (props.ContentLength != req.SizeBytes) return BadRequest("Size mismatch");
        if (!string.IsNullOrEmpty(req.ContentMD5Base64) && props.ContentMD5Base64 is string md5 &&
            md5 != req.ContentMD5Base64)
            return BadRequest("Checksum mismatch");


        var ext = Path.GetExtension(req.FileName ?? "");
        var finalKey = $"entries/{entry.Id}/{Guid.NewGuid():N}{ext}";
        await _storage.PromoteAsync(req.BlobKey, finalKey);


        var att = new JournalAttachment
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            Type = req.Type,
            BlobKey = finalKey,
            FileName = req.FileName,
            MimeType = req.MimeType,
            SizeBytes = req.SizeBytes,
            DurationSec = req.DurationSec,
            Width = req.Width,
            Height = req.Height
        };


        _db.Add(att);
// remove presign log if present
        var log = await _db.Set<PresignedUploadLogs>().FirstOrDefaultAsync(x => x.BlobKey == req.BlobKey);
        if (log is not null) _db.Remove(log);
        await _db.SaveChangesAsync();


        _log.LogInformation("Committed attachment {AttachmentId} for entry {EntryId} ({FinalKey})", att.Id, entry.Id,
            finalKey);
        return Ok(att);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(Guid attachmentId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user id claim.");
        }

        var att = await _db.Set<JournalAttachment>()
            .Include(a => a.Entry).ThenInclude(e => e.UserId)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Entry.UserId == userId);
        if (att is null) return NotFound();


        await _storage.DeleteAsync(att.BlobKey);
        _db.Remove(att);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}