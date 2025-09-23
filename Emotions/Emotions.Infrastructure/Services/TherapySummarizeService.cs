// Emotions.Infrastructure/Services/TherapySummaryService.cs

using System.Text;
using Emotions.Application.DTOs.Therapy;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Security;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

// ✅ OpenAI .NET v2.4.0

namespace Emotions.Infrastructure.Services
{
    public sealed class TherapySummaryService : ITherapySummaryService
    {
        private readonly ChatClient _chat; // inject ChatClient (not OpenAIClient)
        private readonly AppDbContext _db;
        private readonly ITextProtector _protector;

        private const string ModelName = "gpt-4o-mini"; // stored for consistency if you log it

        public TherapySummaryService(ChatClient chat, AppDbContext db, ITextProtector protector)
        {
            _chat = chat;
            _db = db;
            _protector = protector;
        }

        public async Task<SummaryResponse> SummarizeAsync(Guid conversationId, Guid userId, int lastK,
            CancellationToken ct)
        {
            // Pull last K messages (chronological), decrypt text, keep role for formatting
            var msgs = await _db.TherapyMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(lastK)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.AuthorType,
                    m.TextEncrypted
                })
                .ToListAsync(ct);

            var transcript = new StringBuilder();
            foreach (var m in msgs)
            {
                var role = m.AuthorType == AuthorType.User ? "User" : "Therapist";
                var text = _protector.Unprotect(m.TextEncrypted ?? "");
                if (!string.IsNullOrWhiteSpace(text))
                    transcript.AppendLine($"{role}: {text}");
            }

            var prompt =
                "Summarize the conversation in 3–5 concise bullet points focused on feelings and themes." +
                " Then propose one small actionable next step in a supportive tone." +
                " Keep the total under 120 words. Return plain text (no markdown headers).";

            string summaryText = "";
            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("You write concise, supportive therapy summaries."),
                ChatMessage.CreateUserMessage($"{prompt}\n\nTranscript:\n{transcript}")
            };

            try
            {
                var result = await _chat.CompleteChatAsync(
                    chatMessages,
                    cancellationToken: ct
                );


                var completion = result.Value; // ChatCompletion

// Text
                summaryText = completion?.Content?.ToString()?.Trim() ?? "";

// Usage
                var tokensIn = completion?.Usage?.InputTokenCount;
                var tokensOut = completion?.Usage?.OutputTokenCount;

                var cc = result.Value;
                summaryText = result.Value.Content[0].Text.Trim() ?? "";
            }
            catch
            {
                summaryText = "";
            }

            if (string.IsNullOrWhiteSpace(summaryText))
            {
                summaryText =
                    "• You shared meaningful feelings today.\n" +
                    "• A tiny next step: take a 5-minute walk and note one sensation.";
            }

            // Map to your DTO (you can split action later if you want)
            return new SummaryResponse(summary: summaryText, actionTitle: null, actionDetails: null);
        }
    }
}