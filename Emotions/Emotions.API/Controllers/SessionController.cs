using Emotions.Application.DTOs.Rooms;
using Emotions.Application.DTOs.Rooms.Queries;
using Emotions.Application.Interfaces;
using Emotions.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController, Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionsService _sessions;

    public SessionsController(ISessionsService sessions) => _sessions = sessions;

    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] string[]? statuses, [FromQuery] string[]? types, CancellationToken ct = default)
    {
        var q = new SessionsQuery(
            From: from, To: to,
            Statuses: statuses?.Select(s => Enum.Parse<SessionStatus>(s, true)).ToArray(),
            Types: types?.Select(s => Enum.Parse<SessionType>(s, true)).ToArray());

        var (items, total) = await _sessions.ListAsync(q, ct);
        return Ok(new { total, items });
    }

    [HttpGet("{id:guid}")]
    public Task<SessionDto> Get(Guid id, CancellationToken ct) => _sessions.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Host,Admin")]
    public Task<SessionDto> Create([FromBody] CreateSessionRequest req, CancellationToken ct)
        => _sessions.CreateAsync(req, ct);

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Host,Admin")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await _sessions.PublishAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/go-live")]
    [Authorize(Roles = "Host,Admin")]
    public async Task<ActionResult<object>> GoLive(Guid id, CancellationToken ct)
    {
        var voiceRoomId = Guid.NewGuid(); // server-side creation
        await _sessions.GoLiveAsync(id, voiceRoomId, ct); // reuse your existing method
        return Ok(new { voiceRoomId }); // let the client navigate
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Host,Admin")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await _sessions.CompleteAsync(id, ct);
        return NoContent();
    }
}