using System.Security.Claims;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/insights")]
public class InsightsController : ControllerBase
{
    private readonly AppDbContext _db;

    public InsightsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.Insights.Where(i => i.UserId == UserId)
            .OrderByDescending(i => i.CreatedAt).Take(10).ToListAsync();
        return Ok(items.Select(i => new { id = i.Id, text = i.Text, createdAt = i.CreatedAt }));
    }
}