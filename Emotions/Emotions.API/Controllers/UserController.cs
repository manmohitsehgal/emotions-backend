using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // ✅ require a valid JWT for all actions in this controller
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly AppDbContext _db;

    public UsersController(AppDbContext dbContext, IUserService userService)
    {
        _db = dbContext;
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        // ✅ Let the service extract the GUID from claims (and create on first access)
        var user = await _userService.GetOrCreateAsync(ct);

        var interests = await _db.UserInterests
            .Where(ui => ui.UserId == user.Id)
            .Select(ui => ui.Interest.Name)
            .ToListAsync(ct);

        return Ok(new UserDto()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AnalyticsOptIn = user.AnalyticsOptIn,
            HasCompletedOnboarding = user.HasCompletedOnboarding,
            Interests = interests,
        });
    }

    [HttpPost("identify")]
    public async Task<ActionResult<UserDto>> Identify([FromBody] IdentifyUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest("Username is required.");

        var u = await _userService.IdentifyAsync(req.Username.Trim(), ct);
        return Ok(ToResponse(u));
    }


    [HttpPost("onboarding-complete")]
    public async Task<IActionResult> Complete([FromBody] OnboardingCompleteRequest req, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateAsync(ct);

        if (req.AnalyticsOptIn is not null)
            user.AnalyticsOptIn = req.AnalyticsOptIn.Value;

        if (req.Interests is { Count: > 0 })
        {
            // whitelist against catalog
            var wanted = req.Interests
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.ToLowerInvariant())
                .ToHashSet();

            var catalog = await _db.Interests
                .Where(i => wanted.Contains(i.Slug) || wanted.Contains(i.Name.ToLower()))
                .Select(i => i.Id)
                .ToListAsync(ct);

            // replace joins
            var existing = _db.UserInterests.Where(ui => ui.UserId == user.Id);
            _db.UserInterests.RemoveRange(existing);
            _db.UserInterests.AddRange(catalog.Select(id => new UserInterest { UserId = user.Id, InterestId = id }));
        }

        user.HasCompletedOnboarding = true;
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