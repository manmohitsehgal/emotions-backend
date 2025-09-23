using Emotions.Application.DTOs;

namespace Emotions.Application.Interfaces;

public interface IJournalService
{
    Task<JournalEntryDto> CreateAsync(Guid userId, string? title, string text, bool isPrivate,
        CancellationToken ct = default);

    Task<JournalEntryDto?> GetAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntryDto>> ListRecentAsync(Guid userId, int take = 10, CancellationToken ct = default);

    Task<string?> BuildRecentContextAsync(Guid userId, int maxEntries = 10, int maxChars = 1200,
        CancellationToken ct = default);
}