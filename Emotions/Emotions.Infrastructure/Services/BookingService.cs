using Emotions.Application.DTOs.Rooms;
using Emotions.Application.Interfaces;
using Emotions.Application.Pricing;
using Emotions.Domain.Entities;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Emotions.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public class BookingsService : IBookingsService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<VoiceHub, IVoiceClient> _hub;
    private readonly IPremiumService _premium;
    private readonly IWaitlistPriorityCalculator _priority;

    public BookingsService(AppDbContext db, IHubContext<VoiceHub, IVoiceClient> hub, IPremiumService premium,
        IWaitlistPriorityCalculator priority)
    {
        _db = db;
        _hub = hub;
        _premium = premium;
        _priority = priority;
    }

    public async Task<BookingDto> BookAsync(Guid sessionId, Guid userId, bool isPremium, CancellationToken ct)
    {
        var ent = await _premium.GetEntitlementsAsync(userId, ct);
        var priority = _priority.CalculatePriority(userId, ent);

        var s = await _db.SupportSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new InvalidOperationException("Session not found");

        if (s.Status is not SessionStatus.Published)
            throw new InvalidOperationException("Session not open for booking");

        var existing = await _db.SessionBookings
            .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == userId, ct);

        if (existing is not null && existing.Status is not BookingStatus.Cancelled)
            return new BookingDto(sessionId, existing.Status, existing.IsPremium);

        var seatsTaken = await _db.SessionBookings
            .CountAsync(b => b.SessionId == sessionId && b.Status == BookingStatus.Confirmed, ct);

        var status = seatsTaken < s.Capacity ? BookingStatus.Confirmed : BookingStatus.Waitlisted;

        var booking = existing ?? new SessionBooking
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
        };

        booking.Status = status;
        booking.IsPremium = isPremium;
        booking.PriorityScore = priority;
        booking.CreatedAt = DateTime.UtcNow;

        if (existing is null) _db.SessionBookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        await BroadcastSeating(sessionId, ct);
        return new BookingDto(sessionId, booking.Status, isPremium);
    }

    public async Task CancelAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        var booking = await _db.SessionBookings
                          .FirstOrDefaultAsync(b => b.SessionId == sessionId && b.UserId == userId, ct)
                      ?? throw new InvalidOperationException("Booking not found");

        if (booking.Status == BookingStatus.Cancelled) return;

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await PromoteNextFromWaitlist(sessionId, ct);
        await BroadcastSeating(sessionId, ct);
    }

    public async Task<BookingDto?> GetMyBookingAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        var b = await _db.SessionBookings.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == userId, ct);
        return b is null ? null : new BookingDto(sessionId, b.Status, b.IsPremium);
    }

    public async Task<int> AutoReleaseNoShowsAsync(TimeSpan grace, CancellationToken ct)
    {
        // If session is Live and booking is Confirmed but not CheckedIn within grace, release & promote.
        var now = DateTimeOffset.UtcNow;
        var liveSessions = await _db.SupportSessions
            .Where(s => s.Status == SessionStatus.Live && now - s.StartAt > grace)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var toRelease = await _db.SessionBookings
            .Where(b => liveSessions.Contains(b.SessionId)
                        && b.Status == BookingStatus.Confirmed
                        && b.CheckedInAt == null)
            .ToListAsync(ct);

        foreach (var b in toRelease) b.Status = BookingStatus.Cancelled;

        var changed = await _db.SaveChangesAsync(ct);

        foreach (var sid in liveSessions)
        {
            await PromoteNextFromWaitlist(sid, ct);
            await BroadcastSeating(sid, ct);
        }

        return changed;
    }

    public async Task CheckInAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        var b = await _db.SessionBookings
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId
                                              && x.UserId == userId
                                              && x.Status == BookingStatus.Confirmed, ct)
                ?? throw new InvalidOperationException("No confirmed booking for this session.");

        if (b.CheckedInAt is null)
        {
            b.CheckedInAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task PromoteNextFromWaitlist(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.SupportSessions.FirstAsync(x => x.Id == sessionId, ct);
        var seatsTaken =
            await _db.SessionBookings.CountAsync(b => b.SessionId == sessionId && b.Status == BookingStatus.Confirmed,
                ct);
        var seatsFree = s.Capacity - seatsTaken;
        if (seatsFree <= 0) return;

        var waitlisted = await _db.SessionBookings
            .Where(b => b.SessionId == sessionId && b.Status == BookingStatus.Waitlisted)
            .OrderByDescending(b => b.PriorityScore).ThenBy(b => b.CreatedAt)
            .Take(seatsFree)
            .ToListAsync(ct);

        foreach (var w in waitlisted) w.Status = BookingStatus.Confirmed;
        if (waitlisted.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            // Notify promoted users (client can poll MyBooking; add push/email later)
            foreach (var w in waitlisted)
                await _hub.Clients.User(w.UserId.ToString()).BookingPromoted(sessionId);
        }
    }

    private async Task BroadcastSeating(Guid sessionId, CancellationToken ct)
    {
        var seats = await _db.SessionBookings.CountAsync(
            b => b.SessionId == sessionId && b.Status == BookingStatus.Confirmed, ct);
        var waits = await _db.SessionBookings.CountAsync(
            b => b.SessionId == sessionId && b.Status == BookingStatus.Waitlisted, ct);
        await _hub.Clients.All.SeatingUpdated(sessionId, seats, waits);
    }
}

//extend premuin items later like 

// var ctx = new WaitlistContext(
//     Returning: await _db.ReturningCohorts.AnyAsync(x => x.SessionId == s.Id && x.UserId == userId, ct),
//     AttendanceStreak: await _stats.GetStreakAsync(userId, ct),   // your own helper later
//     RecentNoShows: await _db.SessionBookings
//         .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.NoShow 
//                                             && b.CreatedAt >= DateTime.UtcNow.AddDays(30), ct),
//     HostInvite: false // or from a flag
// );
//
// var priority = _priority.CalculatePriority(userId, ent, ctx);
//
//
// public int CalculatePriority(Guid userId, Entitlements ent, WaitlistContext? ctx = null)
// {
//     var score = (ent.IsPremium ? 100 : 0) + ent.PriorityBoost;
//
//     if (ctx is not null)
//     {
//         if (ctx.Returning) score += 20;
//         score += Math.Min(ctx.AttendanceStreak, 5) * 5; // cap streak
//         score -= Math.Min(ctx.RecentNoShows, 3) * 15;   // cap penalty
//         if (ctx.HostInvite) score += 30;
//     }
//     return score;
// }