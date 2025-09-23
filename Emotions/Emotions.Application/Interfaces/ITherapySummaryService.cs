using Emotions.Application.DTOs.Therapy;

namespace Emotions.Application.Interfaces;

public interface ITherapySummaryService
{
    Task<SummaryResponse> SummarizeAsync(Guid conversationId, Guid userId, int lastK, CancellationToken ct);
}