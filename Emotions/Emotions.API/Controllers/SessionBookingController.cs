using Emotions.Application.DTOs.Rooms;
using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/sessions/{sessionId:guid}")]
public class SessionBookingsController : ControllerBase
{
    private readonly IBookingsService _bookings;
    private readonly IUserService _user;

    public SessionBookingsController(IBookingsService bookings, IUserService user)
    {
        _bookings = bookings;
        _user = user;
    }

    [HttpPost("book")]
    [Authorize]
    public async Task<ActionResult<BookingDto>> Book(Guid sessionId, CancellationToken ct)
    {
        var userId = _user.GetAuthenticatedUserId(User);

        // If you have a premium check, prefer the async version:
        // var isPremium = await _user.IsPremiumAsync(User, ct);
        // If not yet implemented, keep false for now:
        var isPremium = false;

        var dto = await _bookings.BookAsync(sessionId, userId, isPremium, ct);
        return Ok(dto);
    }

    [HttpPost("cancel-booking")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid sessionId, CancellationToken ct)
    {
        var userId = _user.GetAuthenticatedUserId(User);
        await _bookings.CancelAsync(sessionId, userId, ct);
        return NoContent();
    }

    [HttpGet("my-booking")]
    [Authorize]
    public Task<BookingDto?> My(Guid sessionId, CancellationToken ct)
    {
        var userId = _user.GetAuthenticatedUserId(User);
        return _bookings.GetMyBookingAsync(sessionId, userId, ct);
    }

    [HttpPost("check-in")]
    [Authorize]
    public async Task<IActionResult> CheckIn(Guid sessionId, CancellationToken ct)
    {
        var userId = _user.GetAuthenticatedUserId(User);
        await _bookings.CheckInAsync(sessionId, userId, ct);
        return NoContent();
    }
}