using Emotions.Application.DTOs.Rooms;
using Emotions.Application.DTOs.Rooms.Queries;
using Emotions.Application.Interfaces;
using Emotions.Application.Mappers;
using Emotions.Domain.Entities;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Emotions.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public class SessionsService : ISessionsService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<VoiceHub, IVoiceClient> _hub; // assume existing

    public SessionsService(AppDbContext db, IHubContext<VoiceHub, IVoiceClient> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<(IReadOnlyList<SessionDto> Items, int Total)> ListAsync(SessionsQuery q, CancellationToken ct)
    {
        var query = _db.SupportSessions
            .Include(s => s.Host)
            .AsQueryable();

        if (q.From is { } from) query = query.Where(s => s.StartAt >= from);
        if (q.To is { } to) query = query.Where(s => s.StartAt < to);
        if (q.Statuses?.Length > 0) query = query.Where(s => q.Statuses!.Contains(s.Status));
        if (q.Types?.Length > 0) query = query.Where(s => q.Types!.Contains(s.Type));

        var total = await query.CountAsync(ct);
        var list = await query.OrderBy(s => s.StartAt).Take(200).ToListAsync(ct);

        var sessionIds = list.Select(s => s.Id).ToArray();
        var counts = await _db.SessionBookings
            .Where(b => sessionIds.Contains(b.SessionId))
            .GroupBy(b => new { b.SessionId, b.Status })
            .Select(g => new { g.Key.SessionId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var dict = counts.GroupBy(x => x.SessionId).ToDictionary(
            g => g.Key,
            g => new
            {
                Seats = g.Where(x => x.Status == BookingStatus.Confirmed).Sum(x => x.Count),
                Waits = g.Where(x => x.Status == BookingStatus.Waitlisted).Sum(x => x.Count)
            });

        var items = list.Select(s =>
        {
            var c = dict.TryGetValue(s.Id, out var v) ? v : new { Seats = 0, Waits = 0 };
            return s.ToDto(c.Seats, c.Waits);
        }).ToList();

        return (items, total);
    }

    public async Task<SessionDto> CreateAsync(CreateSessionRequest req, CancellationToken ct)
    {
        var host = await _db.SessionHosts.FindAsync(new object?[] { req.HostId }, ct)
                   ?? throw new InvalidOperationException("Host not found");

        var entity = new SupportSession
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Description = req.Description,
            Type = req.Type,
            Status = SessionStatus.Draft,
            SpeakPolicy = req.SpeakPolicy,
            StartAt = req.StartAt,
            EndAt = req.EndAt,
            Capacity = Math.Max(1, req.Capacity),
            AllowListeners = req.AllowListeners,
            HostId = req.HostId,
            TemplateId = req.TemplateId,
        };

        _db.SupportSessions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.ToDto(0, 0);
    }

    public async Task PublishAsync(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.SupportSessions.Include(x => x.Host)
                    .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new InvalidOperationException("Session not found");

        if (s.Status != SessionStatus.Draft) return;
        s.Status = SessionStatus.Published;
        s.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _hub.Clients.All.SessionPublished(s.Id, s.StartAt);
    }

    public async Task GoLiveAsync(Guid sessionId, Guid voiceRoomId, CancellationToken ct)
    {
        var s = await _db.SupportSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new InvalidOperationException("Session not found");

        s.Status = SessionStatus.Live;
        s.VoiceRoomId = voiceRoomId;
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.All.SessionLive(s.Id, voiceRoomId);
    }

    public async Task CompleteAsync(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.SupportSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new InvalidOperationException("Session not found");

        s.Status = SessionStatus.Completed;
        s.CompletedAt = DateTime.UtcNow;
        s.VoiceRoomId = null;
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.All.SessionCompleted(s.Id);
    }

    public async Task<SessionDto> GetAsync(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.SupportSessions.Include(x => x.Host)
                    .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
                ?? throw new InvalidOperationException("Session not found");

        var seats = await _db.SessionBookings.CountAsync(
            b => b.SessionId == sessionId && b.Status == BookingStatus.Confirmed, ct);
        var waits = await _db.SessionBookings.CountAsync(
            b => b.SessionId == sessionId && b.Status == BookingStatus.Waitlisted, ct);
        return s.ToDto(seats, waits);
    }
}