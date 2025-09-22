namespace Emotions.Application.Interfaces.AI.Records;

public record AiSummaryRequest
{
    public Guid ConversationId { get; init; }
    public IEnumerable<(string role, string text)> Transcript { get; init; } = Enumerable.Empty<(string, string)>();
}