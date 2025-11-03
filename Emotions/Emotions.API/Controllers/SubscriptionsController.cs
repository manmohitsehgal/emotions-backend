using System.Security.Claims;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController(AppDbContext db) : ControllerBase
    {
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = UserId;

            var sub = await db.UserSubscriptions.SingleOrDefaultAsync(x => x.UserId == userId);

            var setting = await db.AppSettings
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == "PaywallOverride");

            var adminOverride = string.Equals(setting?.Value, "true", StringComparison.OrdinalIgnoreCase);

            var roleBypass = User.IsInRole("Admin") || User.IsInRole("FreeUser");

            var premium = sub?.Premium ?? false;
            var freeTrialActive = sub?.FreeTrialActive ?? false;

            var premiumEffective = roleBypass || adminOverride || premium || freeTrialActive;

            return Ok(new
            {
                premium,
                freeTrialActive,
                freeTrialEndsAt = sub?.FreeTrialEndsAt,
                adminOverride,
                roleBypass,
                premiumEffective
            });
        }

        public record ActivateReq(bool mock);

        [HttpPost("free-trial")]
        public async Task<IActionResult> StartFreeTrial()
        {
            var sub = await db.UserSubscriptions.SingleOrDefaultAsync(x => x.UserId == UserId);
            if (sub is null)
            {
                sub = new UserSubscription { UserId = UserId };
                db.UserSubscriptions.Add(sub);
            }

            if (!sub.Premium)
            {
                sub.FreeTrialActive = true;
                sub.FreeTrialEndsAt = DateTime.UtcNow.AddDays(7);
            }

            await db.SaveChangesAsync();
            return await Me();
        }

        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateReq req)
        {
            var sub = await db.UserSubscriptions.SingleOrDefaultAsync(x => x.UserId == UserId);
            if (sub is null)
            {
                sub = new UserSubscription { UserId = UserId };
                db.UserSubscriptions.Add(sub);
            }

            sub.Premium = true;
            sub.FreeTrialActive = false;
            sub.FreeTrialEndsAt = null;
            await db.SaveChangesAsync();
            return await Me();
        }
    }
}