using Emotions.Application.DTOs.Therapy;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Security;
using Emotions.Domain.Entities;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;

namespace Emotions.Infrastructure.Services;

public sealed class TherapyChatService : ITherapyChatService
{
    private readonly ChatClient _chat;
    private readonly AppDbContext _db;
    private readonly ITextProtector _protector;

    private const string Model = "gpt-4o-mini";

    private const string SystemPrompt =
        "You are a supportive, CBT-informed assistant. Be empathetic, concise, and suggest one small actionable next step when helpful.";

    private const string Fallback =
        "Thanks for sharing. I’m here with you. What feels most present right now?";

    public TherapyChatService(OpenAIClient client, AppDbContext db, ITextProtector protector)
    {
        _chat = client.GetChatClient(Model);
        _db = db;
        _protector = protector;
    }

    public async Task<MessageResponse> SendAsync(Guid conversationId, Guid userId, string userText,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Persist user msg
        _db.TherapyMessages.Add(new TherapyMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            AuthorType = AuthorType.User,
            TextEncrypted = _protector.Protect(userText ?? ""),
            CreatedAt = now,
            IsPrivate = false
        });
        await _db.SaveChangesAsync(ct);

        // Safety triage
        var severity = SafetyHeuristics.Classify(userText);
        if (severity == SafetySeverity.Crisis)
        {
            var crisis =
                "I’m really sorry you’re feeling this way. If you’re in immediate danger, please call your local emergency number now. In the U.S., you can dial or text 988 to reach the Suicide & Crisis Lifeline. Would you like a grounding exercise?";
            return await PersistAssistantAndReturn(conversationId, crisis, now, ct);
        }

        // Context: last 8 msgs
        var history = await _db.TherapyMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(8)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.AuthorType, m.TextEncrypted })
            .ToListAsync(ct);

        var msgs = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(SystemPrompt)
        };

        foreach (var h in history)
        {
            var content = _protector.Unprotect(h.TextEncrypted ?? "");
            msgs.Add(h.AuthorType == AuthorType.User
                ? ChatMessage.CreateUserMessage(content)
                : ChatMessage.CreateAssistantMessage(content));
        }

        msgs.Add(ChatMessage.CreateUserMessage(userText ?? ""));

        // Call OpenAI
        string assistant;
        int? tokensIn = null, tokensOut = null;

        try
        {
            var result = await _chat.CompleteChatAsync(messages: msgs, cancellationToken: ct);
            var completion = result.Value; // ChatCompletion
            assistant = completion.Content[0].Text;
            // tokensIn = cc?.Usage?.InputTokenCount;
            // tokensOut = cc?.Usage?.OutputTokenCount;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            assistant = "";
        }

        if (string.IsNullOrWhiteSpace(assistant))
            assistant = Fallback;

        return await PersistAssistantAndReturn(conversationId, assistant, DateTime.UtcNow, ct, tokensIn, tokensOut);
    }

    private async Task<MessageResponse> PersistAssistantAndReturn(
        Guid conversationId,
        string text,
        DateTime createdAt,
        CancellationToken ct,
        int? tokensIn = null,
        int? tokensOut = null)
    {
        var id = Guid.NewGuid();

        _db.TherapyMessages.Add(new TherapyMessage
        {
            Id = id,
            ConversationId = conversationId,
            AuthorType = AuthorType.Therapist,
            TextEncrypted = _protector.Protect(text ?? ""),
            CreatedAt = createdAt,
            IsPrivate = false,
            Model = Model,
            TokensIn = tokensIn,
            TokensOut = tokensOut
        });

        await _db.SaveChangesAsync(ct);

        return new MessageResponse
        {
            Id = id,
            Role = "therapist",
            Text = text,
            CreatedAt = createdAt
        };
    }
}

internal static class SafetyHeuristics
{
    public static SafetySeverity Classify(string? t)
    {
        t = (t ?? "").ToLowerInvariant();
        if (t.Contains("kill myself") || t.Contains("suicide") || t.Contains("end my life") || t.Contains("overdose"))
            return SafetySeverity.Crisis;
        if (t.Contains("no point") || t.Contains("hopeless"))
            return SafetySeverity.Concern;
        return SafetySeverity.Normal;
    }
}

internal enum SafetySeverity
{
    Normal,
    Concern,
    Crisis
}