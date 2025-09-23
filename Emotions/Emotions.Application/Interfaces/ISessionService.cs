using Emotions.Application.DTOs.Rooms;
using Emotions.Application.DTOs.Rooms.Queries;

namespace Emotions.Application.Interfaces;

public interface ISessionsService
{
    Task<(IReadOnlyList<SessionDto> Items, int Total)> ListAsync(SessionsQuery q, CancellationToken ct);
    Task<SessionDto> CreateAsync(CreateSessionRequest req, CancellationToken ct);
    Task PublishAsync(Guid sessionId, CancellationToken ct);
    Task GoLiveAsync(Guid sessionId, Guid voiceRoomId, CancellationToken ct);
    Task CompleteAsync(Guid sessionId, CancellationToken ct);
    Task<SessionDto> GetAsync(Guid sessionId, CancellationToken ct);
}