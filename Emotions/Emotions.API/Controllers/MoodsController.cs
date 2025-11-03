using System.Security.Claims;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers
{
    [ApiController]
    [Route("api/moods")]
    public class MoodsController(AppDbContext db) : ControllerBase
    {
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public record MoodRequest(double value);

        public record AffectRequest(int tone, int energy);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MoodRequest req)
        {
            db.Moods.Add(new MoodEntry { UserId = UserId, Value = Math.Clamp(req.value, 0, 1) });
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("affect")]
        public async Task<IActionResult> Affect([FromBody] AffectRequest req)
        {
            var clampedTone = Math.Clamp(req.tone, 0, 2);
            var clampedEnergy = Math.Clamp(req.energy, 0, 2);
            var value = (clampedEnergy / 2.0) * 0.7 + (clampedTone / 2.0) * 0.3;
            db.Moods.Add(new MoodEntry { UserId = UserId, Value = value });
            await db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("streak")]
        public async Task<IActionResult> Streak()
        {
            var userId = UserId;
            var today = DateTime.UtcNow.Date;
            var entries = await db.Moods
                .Where(m => m.UserId == userId && m.CreatedAt >= today.AddDays(-30))
                .Select(m => m.CreatedAt.Date).Distinct().ToListAsync();

            int streak = 0;
            for (int i = 0; i < 31; i++)
            {
                var d = today.AddDays(-i);
                if (entries.Contains(d)) streak++;
                else break;
            }

            return Ok(new { value = streak });
        }
    }
}