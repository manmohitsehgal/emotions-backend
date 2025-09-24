using Emotions.Application.DTOs;

namespace Emotions.Application.Interfaces;

public interface IJournalService
{
    Task<JournalEntryDto> CreateAsync(Guid userId, CreateJournalEntryRequestDto req, CancellationToken ct = default);
    Task<JournalEntryDto?> GetAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<JournalEntryDto>> ListAsync(Guid userId, int limit = 20, string? cursor = null,
        CancellationToken ct = default);

    Task<JournalEntryDto?> UpdateAsync(Guid userId, Guid id, UpdateJournalEntryRequestDto req,
        CancellationToken ct = default);

    Task<bool> ArchiveAsync(Guid userId, Guid id, CancellationToken ct = default);
}