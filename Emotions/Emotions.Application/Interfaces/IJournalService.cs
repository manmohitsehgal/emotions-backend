using Emotions.Application.DTOs;
using Emotions.Domain.Entities;

namespace Emotions.Application.Interfaces;

public interface IJournalService
{
    Task<JournalEntry> CreateAsync(CreateJournalEntryDTO dto);
    Task<List<JournalEntry>> GetAllAsync(string userId);
}