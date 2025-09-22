using Emotions.Application.Interfaces.AI.Records;

namespace Emotions.Application.Interfaces;

public interface IAiService
{
    Task<AiTurnResponse> TherapyRespondAsync(AiTurnRequest req, CancellationToken ct);
    Task<AiSummaryResponse> TherapySummarizeAsync(AiSummaryRequest req, CancellationToken ct);
    Task<SafetyCheckResponse> SafetyCheckAsync(string text, CancellationToken ct);
}