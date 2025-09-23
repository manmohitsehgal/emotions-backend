using Emotions.Application.DTOs.Therapy;

namespace Emotions.Application.Interfaces;

public interface ITherapyChatService
{
    Task<MessageResponse> SendAsync(Guid conversationId, Guid userId, string text, CancellationToken ct = default);

    // Task<SummaryResponse> SummarizeAsync(Guid conversationId, Guid userId, int lastK = 20,
    //     CancellationToken ct = default);
}