using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[Authorize]
[ApiController]
[Route("api/journal")]
public class JournalController : ControllerBase
{
    private readonly IJournalService _journal;
    private readonly IUserService _users;

    public JournalController(IJournalService journal, IUserService user)
    {
        _journal = journal;
        _users = user;
    }


    [HttpPost("entries")]
    public async Task<ActionResult<JournalEntryDto>> CreateEntry([FromBody] CreateJournalEntryRequestDto req)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var dto = await _journal.CreateAsync(userId, req);
        return CreatedAtAction(nameof(GetEntry), new { id = dto.Id }, dto);
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyList<JournalEntryDto>>> ListEntries([FromQuery] int limit = 20)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var list = await _journal.ListAsync(userId, limit);
        return Ok(list);
    }

    [HttpGet("entries/{id}")]
    public async Task<ActionResult<JournalEntryDto>> GetEntry([FromRoute] Guid id)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var dto = await _journal.GetAsync(userId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("entries/{id}")]
    public async Task<ActionResult<JournalEntryDto>> UpdateEntry([FromRoute] Guid id,
        [FromBody] UpdateJournalEntryRequestDto req)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var dto = await _journal.UpdateAsync(userId, id, req);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("entries/{id}")]
    public async Task<IActionResult> ArchiveEntry([FromRoute] Guid id)
    {
        var userId = _users.GetAuthenticatedUserId(User);
        var ok = await _journal.ArchiveAsync(userId, id);
        return ok ? NoContent() : NotFound();
    }
}