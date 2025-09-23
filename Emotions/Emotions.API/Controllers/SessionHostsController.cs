using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/session-hosts")]
public sealed class SessionHostsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public SessionHostsController(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(sub)) return Unauthorized();

        var host = await _db.SessionHosts.AsNoTracking()
            .Where(h => !h.IsAi && h.UserId != null)
            .Join(_db.Users, h => h.UserId, u => u.Id, (h, u) => new { h, u })
            .Where(x => x.u.ExternalId == sub)
            .Select(x => new { id = x.h.Id, displayName = x.h.DisplayName })
            .SingleOrDefaultAsync(ct);

        if (host is null) return NotFound();
        return Ok(host);
    }
}