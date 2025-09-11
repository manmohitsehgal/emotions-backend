using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // ✅ require a valid JWT for all actions in this controller
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly AppDbContext _db;

    public UsersController(AppDbContext dbContext, IUserService users)
    {
        _db = dbContext;
        _users = users;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // ✅ Let the service extract the GUID from claims (and create on first access)
        var u = await _users.GetOrCreateAsync(ct);

        return Ok(new
        {
            id = u.Id,
            username = u.Username,
            hasCompletedOnboarding = u.HasCompletedOnboarding,
            analyticsOptIn = u.AnalyticsOptIn,
            // Optionally include email if you store it:
            // email = u.Email
        });
    }

    [HttpPost("identify")]
    public async Task<ActionResult<UserDto>> Identify([FromBody] IdentifyUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest("Username is required.");

        var u = await _users.IdentifyAsync(req.Username.Trim(), ct);
        return Ok(ToResponse(u));
    }

    public record OnboardingReq(bool analyticsOptIn);

    [HttpPost("onboarding-complete")]
    public async Task<IActionResult> Complete([FromBody] OnboardingReq r, CancellationToken ct)
    {
        // ✅ Same: get current user via service; update flags
        var u = await _users.GetOrCreateAsync(ct);
        u.HasCompletedOnboarding = true;
        u.AnalyticsOptIn = r.analyticsOptIn;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static UserDto ToResponse(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        AnalyticsOptIn = u.AnalyticsOptIn,
        HasCompletedOnboarding = u.HasCompletedOnboarding
    };
}