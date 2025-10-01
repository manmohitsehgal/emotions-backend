using Emotions.Application.Common;
using Emotions.Application.DTOs;
using Emotions.Application.DTOs.Rooms;
using Emotions.Application.DTOs.Rooms.Queries;

namespace Emotions.Application.Interfaces;

public interface IRoomService
{
    Task<PagedResult<RoomSummaryDto>> ListAsync(RoomListQuery query, CancellationToken ct = default);
    Task<RoomDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ParticipantsListDto> GetParticipantsAsync(Guid id, CancellationToken ct = default);
}