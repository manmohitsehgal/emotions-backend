using Emotions.Application.Common;
using Emotions.Application.DTOs;
using Emotions.Application.DTOs.VoiceRooms;
using Emotions.Application.DTOs.VoiceRooms.Queries;

namespace Emotions.Application.Interfaces;

public interface IVoiceRoomService
{
    Task<PagedResult<VoiceRoomSummaryDto>> ListAsync(RoomListQuery query, CancellationToken ct = default);
    Task<VoiceRoomDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ParticipantsListDto> GetParticipantsAsync(Guid id, CancellationToken ct = default);
}