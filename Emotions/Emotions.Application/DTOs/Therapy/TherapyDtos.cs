namespace Emotions.Application.DTOs.Therapy;

public record CreateConversationRequest(bool includeJournal = false);

public record ConversationResponse(Guid id, bool includeJournal, string mode, DateTime startedAt);

public record SendMessageRequest(string text);

public sealed class MessageResponse
{
    public Guid Id { get; init; }
    public string Role { get; init; } = "therapist"; // UI wants therapist label
    public string Text { get; init; } = ""; // <-- THIS must be non-empty
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record SummaryResponse(string summary, string? actionTitle, string? actionDetails);