using Emotions.Application.DTOs;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public class JournalService : IJournalService
{
    private readonly AppDbContext _context;

    public JournalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JournalEntry> CreateAsync(CreateJournalEntryDTO dto)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Text = dto.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<List<JournalEntry>> GetAllAsync(string userId)
    {
        return await _context.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }
}