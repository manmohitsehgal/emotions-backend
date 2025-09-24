using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Security;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public class JournalService : IJournalService
{
    private readonly AppDbContext _db;
    private readonly ITextProtector _protector; // existing app service

    public JournalService(AppDbContext db, ITextProtector protector)
    {
        _db = db;
        _protector = protector;
    }

    public async Task<JournalEntryDto> CreateAsync(Guid userId, CreateJournalEntryRequestDto req,
        CancellationToken ct = default)
    {
        var privacy = await _db.JournalPrivacy.FindAsync(new object?[] { userId }, ct) ??
                      new JournalPrivacy { UserId = userId };
        var entity = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = req.Title,
            BodyCipher = _protector.Protect(req.Body ?? string.Empty),
            Mode = "text",
            Mood = req.Mood,
            Privacy = req.Privacy ?? privacy.DefaultPrivacy,
            CreatedAt = DateTime.UtcNow
        };
        _db.JournalEntries.Add(entity);


// streaks
        var streak = await _db.StreakCounters.FindAsync(new object?[] { userId }, ct) ??
                     new StreakCounter { UserId = userId };
        if (streak.LastEntryAt.HasValue && streak.LastEntryAt.Value.Date.AddDays(1) == DateTime.UtcNow.Date)
            streak.CurrentStreak++;
        else if (streak.LastEntryAt?.Date != DateTime.UtcNow.Date)
            streak.CurrentStreak = 1;
        streak.BestStreak = Math.Max(streak.BestStreak, streak.CurrentStreak);
        streak.LastEntryAt = DateTime.UtcNow;
        _db.StreakCounters.Update(streak);


        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<JournalEntryDto?> GetAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var e = await _db.JournalEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsArchived, ct);
        return e is null ? null : ToDto(e);
    }

    public async Task<IReadOnlyList<JournalEntryDto>> ListAsync(Guid userId, int limit = 20, string? cursor = null,
        CancellationToken ct = default)
    {
        var q = _db.JournalEntries.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsArchived)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit);
        var list = await q.ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<JournalEntryDto?> UpdateAsync(Guid userId, Guid id, UpdateJournalEntryRequestDto req,
        CancellationToken ct = default)
    {
        var e = await _db.JournalEntries.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsArchived,
            ct);
        if (e is null) return null;
        if (req.Title is not null) e.Title = req.Title;
        if (req.Body is not null) e.BodyCipher = _protector.Protect(req.Body);
        if (req.Mood is not null) e.Mood = req.Mood;
        if (req.Privacy is not null) e.Privacy = req.Privacy;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<bool> ArchiveAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var e = await _db.JournalEntries.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id && !x.IsArchived,
            ct);
        if (e is null) return false;
        e.IsArchived = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private JournalEntryDto ToDto(JournalEntry e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Body = _protector.Unprotect(e.BodyCipher),
        Mode = e.Mode,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Mood = e.Mood,
        Privacy = e.Privacy
    };
}