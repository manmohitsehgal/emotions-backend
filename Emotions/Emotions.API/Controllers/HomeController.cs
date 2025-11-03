using System.Security.Claims;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("snapshot")]
    public async Task<IActionResult> Snapshot()
    {
        var now = DateTime.UtcNow;
        var today = await _db.Moods
            .Where(m => m.UserId == UserId && m.CreatedAt >= now.Date)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        // naive energy: avg of last 5 moods
        var last = await _db.Moods.Where(m => m.UserId == UserId)
            .OrderByDescending(m => m.CreatedAt).Take(5).ToListAsync();
        var energy = last.Count == 0 ? 0.5 : last.Average(m => m.Value);

        return Ok(new
        {
            moodToday = today is null ? null : new { value = today.Value, at = today.CreatedAt, label = "" },
            energy
        });
    }
}