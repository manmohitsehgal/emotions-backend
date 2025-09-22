using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserService _users;

    public UsersController(AppDbContext db, IUserService users)
    {
        _db = db;
        _users = users;
    }

    // GET /api/users/me — pure read; never creates a user
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await _users.TryGetByExternalIdAsync(User, ct);
        if (user is null) return NotFound();

        var interests = await _db.UserInterests
            .Where(ui => ui.UserId == user.Id)
            .Select(ui => ui.Interest.Slug)
            .ToListAsync(ct);

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AnalyticsOptIn = user.AnalyticsOptIn,
            HasCompletedOnboarding = user.HasCompletedOnboarding,
            Interests = interests
        };

        // ✅ Add roles from the token (namespaced claim configured in Program.cs)
        var roles = User.FindAll("https://emotions.app/roles").Select(c => c.Value).ToArray();

        return Ok(new
            { dto.Id, dto.Username, dto.Email, dto.AnalyticsOptIn, dto.HasCompletedOnboarding, dto.Interests, roles });
    }

    // POST /api/users/provision — idempotent create after interactive login
    [Authorize]
    [HttpPost("provision")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Provision(CancellationToken ct)
    {
        var user = await _users.ProvisionFromClaimsAsync(User, ct);

        var interests = await _db.UserInterests
            .Where(ui => ui.UserId == user.Id)
            .Select(ui => ui.Interest.Slug)
            .ToListAsync(ct);

        var dto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AnalyticsOptIn = user.AnalyticsOptIn,
            HasCompletedOnboarding = user.HasCompletedOnboarding,
            Interests = interests
        };
        return Ok(dto);
    }

    // POST /api/users/username — set/replace app username
    [Authorize]
    [HttpPost("username")]
    public async Task<IActionResult> SetUsername([FromBody] SetUsernameRequest req, CancellationToken ct)
    {
        await _users.SetUsernameAsync(User, req.Username, ct);
        return NoContent();
    }

    // POST /api/users/interests — replace interests by slug list
    [Authorize]
    [HttpPost("interests")]
    public async Task<IActionResult> SetInterests([FromBody] SetInterestsRequest req, CancellationToken ct)
    {
        await _users.SetInterestsAsync(User, req.Interests ?? Enumerable.Empty<string>(), ct);
        return NoContent();
    }

    // POST /api/users/onboarding-complete — mark onboarding done
    [Authorize]
    [HttpPost("onboarding-complete")]
    public async Task<IActionResult> Complete([FromBody] OnboardingCompleteRequest req, CancellationToken ct)
    {
        await _users.CompleteOnboardingAsync(User, req.AnalyticsOptIn, ct);
        return NoContent();
    }
}

// ---------- Contracts used by this controller ----------
public sealed class SetUsernameRequest
{
    public required string Username { get; set; }
}

public sealed class SetInterestsRequest
{
    public List<string>? Interests { get; set; }
}

public sealed class OnboardingCompleteRequest
{
    public bool? AnalyticsOptIn { get; set; }
}