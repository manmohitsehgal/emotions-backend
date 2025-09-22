using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Security;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
// IEncryptionService

namespace Emotions.Infrastructure.Services;

public sealed class JournalService : IJournalService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;

    public JournalService(AppDbContext db, IEncryptionService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    public async Task<JournalEntryDto> CreateAsync(Guid userId, string? title, string text, bool isPrivate,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var entity = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            TextEncrypted = await _crypto.EncryptAsync(text, ct),
            IsPrivate = isPrivate,
            CreatedAt = DateTime.UtcNow
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new JournalEntryDto(entity.Id, entity.UserId, entity.Title, text, entity.IsPrivate, entity.CreatedAt,
            entity.UpdatedAt);
    }

    public async Task<JournalEntryDto?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var e = await _db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (e is null) return null;
        var text = await _crypto.DecryptAsync(e.TextEncrypted, ct);
        return new JournalEntryDto(e.Id, e.UserId, e.Title, text, e.IsPrivate, e.CreatedAt, e.UpdatedAt);
    }

    public async Task<IReadOnlyList<JournalEntryDto>> ListRecentAsync(Guid userId, int take = 10,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 50);
        var items = await _db.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        var result = new List<JournalEntryDto>(items.Count);
        foreach (var e in items.OrderBy(j => j.CreatedAt)) // oldest→newest for better flow
        {
            var text = await _crypto.DecryptAsync(e.TextEncrypted, ct);
            result.Add(new JournalEntryDto(e.Id, e.UserId, e.Title, text, e.IsPrivate, e.CreatedAt, e.UpdatedAt));
        }

        return result;
    }

    public async Task<string?> BuildRecentContextAsync(Guid userId, int maxEntries = 10, int maxChars = 1200,
        CancellationToken ct = default)
    {
        var recent = await ListRecentAsync(userId, maxEntries, ct);
        if (recent.Count == 0) return null;

        var blocks = recent
            .Where(r => !string.IsNullOrWhiteSpace(r.Text) /* && !r.IsPrivate */) // optionally exclude private
            .Select(r =>
            {
                var title = string.IsNullOrWhiteSpace(r.Title) ? "" : $"[{r.Title}] ";
                return $"{title}{r.Text}".Trim();
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (blocks.Count == 0) return null;

        var joined = string.Join("\n---\n", blocks);
        if (joined.Length > maxChars) joined = joined[..maxChars] + "…";
        return $"Recent journal highlights:\n{joined}";
    }
}