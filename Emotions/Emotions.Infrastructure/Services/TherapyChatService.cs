using Emotions.Application.DTOs.Therapy;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.AI.Records;
using Emotions.Application.Interfaces.Security;
using Emotions.Domain.Entities;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public sealed class TherapyChatService : ITherapyChatService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _crypto;
    private readonly IJournalService _journal;
    private readonly IAiService _ai;

    public TherapyChatService(AppDbContext db, IEncryptionService crypto, IJournalService journal, IAiService ai)
    {
        _db = db;
        _crypto = crypto;
        _journal = journal;
        _ai = ai;
    }

    public async Task<MessageResponse> SendAsync(Guid conversationId, Guid userId, string text,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var convo = await _db.TherapyConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);
        if (convo is null) throw new KeyNotFoundException("Conversation not found.");

        // 1) Save user message
        var userMsg = new TherapyMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            AuthorType = AuthorType.User,
            TextEncrypted = await _crypto.EncryptAsync(text, ct),
            CreatedAt = DateTime.UtcNow
        };
        _db.TherapyMessages.Add(userMsg);
        await _db.SaveChangesAsync(ct);

        // 2) Optional journal context
        string? journalContext = convo.IncludeJournal
            ? await _journal.BuildRecentContextAsync(convo.UserId, ct: ct)
            : null;

        // 3) Safety check
        var safety = await _ai.SafetyCheckAsync(text, ct);
        if (string.Equals(safety.level, "Crisis", StringComparison.OrdinalIgnoreCase))
        {
            _db.SafetyEvents.Add(new SafetyEvent
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                MessageId = userMsg.Id,
                Level = SafetyLevel.Crisis,
                TriggeredAt = DateTime.UtcNow
            });
            convo.SafetyLevel = SafetyLevel.Crisis;
            await _db.SaveChangesAsync(ct);
        }
        else if (string.Equals(safety.level, "Watch", StringComparison.OrdinalIgnoreCase))
        {
            convo.SafetyLevel = SafetyLevel.Watch;
            await _db.SaveChangesAsync(ct);
        }

        // 4) AI response  ✅ use object initializer
        var aiResp = await _ai.TherapyRespondAsync(
            new AiTurnRequest
            {
                ConversationId = conversationId,
                UserText = text,
                Mode = convo.Mode.ToString(), // or "Vent" for MVP
                JournalContext = journalContext
            },
            ct);

        // 5) Save assistant message
        var botMsg = new TherapyMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            AuthorType = AuthorType.Assistant,
            TextEncrypted = await _crypto.EncryptAsync(aiResp.Text, ct),
            CreatedAt = DateTime.UtcNow,
            Model = aiResp.Model,
            TokensIn = aiResp.TokensIn,
            TokensOut = aiResp.TokensOut
        };
        _db.TherapyMessages.Add(botMsg);
        await _db.SaveChangesAsync(ct);

        return new MessageResponse(botMsg.Id, "assistant", aiResp.Text, botMsg.CreatedAt);
    }

    public async Task<SummaryResponse> SummarizeAsync(Guid conversationId, Guid userId, int lastK = 20,
        CancellationToken ct = default)
    {
        var convo = await _db.TherapyConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);
        if (convo is null) throw new KeyNotFoundException("Conversation not found.");

        var msgs = await _db.TherapyMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(lastK)
            .ToListAsync(ct);

        if (msgs.Count == 0) throw new InvalidOperationException("No messages to summarize.");

        var transcript = new List<(string role, string text)>(msgs.Count);
        foreach (var m in msgs.OrderBy(m => m.CreatedAt))
        {
            var plain = await _crypto.DecryptAsync(m.TextEncrypted, ct);
            transcript.Add((m.AuthorType.ToString().ToLowerInvariant(), plain));
        }

        // ✅ if AiSummaryRequest is also a POCO, use initializer
        var ai = await _ai.TherapySummarizeAsync(
            new AiSummaryRequest
            {
                ConversationId = conversationId,
                Transcript = transcript
            },
            ct);

        if (!string.IsNullOrWhiteSpace(ai.ActionTitle))
        {
            var anchor = msgs.Last();
            // ✅ DbSet name likely plural
            _db.TherapyActionItem.Add(new TherapyActionItems
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                MessageId = anchor.Id,
                Title = ai.ActionTitle!,
                Details = ai.ActionDetails
            });
            await _db.SaveChangesAsync(ct);
        }

        return new SummaryResponse(ai.Summary, ai.ActionTitle, ai.ActionDetails);
    }
}