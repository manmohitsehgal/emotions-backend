using System.Security.Claims;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers
{
    [ApiController]
    [Route("api/plan")]
    public class PlanController(AppDbContext db) : ControllerBase
    {
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] Guid? planId)
        {
            var userId = UserId;

            Guid resolvedPlanId = planId ?? await db.PlanSteps
                .Join(db.PlanStepStatuses.Where(s => s.UserId == userId),
                    step => step.Id,
                    status => status.PlanStepId,
                    (step, status) => new { step.PlanId, status.CompletedAt })
                .OrderByDescending(x => x.CompletedAt)
                .Select(x => x.PlanId)
                .FirstOrDefaultAsync();

            if (resolvedPlanId == Guid.Empty)
            {
                resolvedPlanId = await db.PlanSteps
                    .Select(s => s.PlanId)
                    .FirstOrDefaultAsync();
            }

            if (resolvedPlanId == Guid.Empty)
                return Ok(Array.Empty<object>());

            var steps = await db.PlanSteps
                .AsNoTracking()
                .Where(s => s.PlanId == resolvedPlanId)
                .OrderBy(s => s.DayNumber)
                .Select(s => new
                {
                    s.Id,
                    title = s.Text,
                    status = db.PlanStepStatuses
                        .Where(ps => ps.PlanStepId == s.Id && ps.UserId == userId)
                        .Select(ps => new { ps.IsDone, ps.CompletedAt })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = steps.Select(s => new
            {
                id = s.Id,
                title = s.title,
                done = s.status?.IsDone ?? false
            });

            return Ok(result);
        }

        [HttpPost("{id:guid}/toggle")]
        public async Task<IActionResult> Toggle(Guid id)
        {
            var userId = UserId;

            var stepExists = await db.PlanSteps
                .AsNoTracking()
                .AnyAsync(x => x.Id == id);

            if (!stepExists) return NotFound();

            var status = await db.PlanStepStatuses.FindAsync(id, userId);
            if (status is null)
            {
                db.PlanStepStatuses.Add(new PlanStepStatus
                {
                    PlanStepId = id,
                    UserId = userId,
                    IsDone = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                status.IsDone = !status.IsDone;
                status.CompletedAt = status.IsDone ? DateTime.UtcNow : (DateTime?)null;
            }

            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}