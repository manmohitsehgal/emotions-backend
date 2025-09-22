namespace Emotions.Application.DTOs.Therapy;

public record CreateConversationRequest(bool includeJournal = false);

public record ConversationResponse(Guid id, bool includeJournal, string mode, DateTime startedAt);

public record SendMessageRequest(string text);

public record MessageResponse(Guid id, string role, string text, DateTime createdAt);

public record SummaryResponse(string summary, string? actionTitle, string? actionDetails);