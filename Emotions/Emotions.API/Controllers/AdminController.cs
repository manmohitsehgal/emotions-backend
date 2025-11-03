using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    public record OverrideReq(bool enabled);

    [HttpPost("paywall-override")]
    public async Task<IActionResult> Override([FromBody] OverrideReq req)
    {
        var s = await _db.AppSettings.SingleOrDefaultAsync(x => x.Key == "PaywallOverride");
        if (s is null)
        {
            s = new AppSetting { Key = "PaywallOverride", Value = req.enabled ? "true" : "false" };
            _db.AppSettings.Add(s);
        }
        else
        {
            s.Value = req.enabled ? "true" : "false";
        }

        await _db.SaveChangesAsync();
        return Ok(new { adminOverride = req.enabled });
    }
}