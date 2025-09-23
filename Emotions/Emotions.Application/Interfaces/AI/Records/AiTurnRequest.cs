namespace Emotions.Application.Interfaces.AI.Records;

public record AiTurnRequest
{
    public Guid ConversationId { get; init; }
    public string UserText { get; init; } = default!;
    public string Mode { get; init; } = "Vent";
    public string? JournalContext { get; init; }
}