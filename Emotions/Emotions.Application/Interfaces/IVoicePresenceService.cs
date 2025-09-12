using Emotions.Application.DTOs.VoiceRooms;

namespace Emotions.Application.Interfaces;

public interface IVoicePresenceService : IAsyncDisposable
{
    // membership / connection lifecycle
    Task<bool> AddAsync(Guid roomId, ParticipantDto participant, string connectionId);
    Task RemoveByConnectionAsync(string connectionId);
    Task RemoveAsync(Guid roomId, Guid userId);

    // queries
    Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(Guid roomId);
    Task<ParticipantDto?> GetAsync(Guid roomId, Guid userId);
    Task<(Guid? roomId, ParticipantDto? participant)> FindByConnectionAsync(string connectionId);

    // attrs
    Task UpdateAsync(Guid roomId, Guid userId, bool? isMuted = null, bool? isVideoOn = null);

    // counts (you already had these)
    int? GetApproxMemberCount(Guid roomId);
    IDictionary<Guid, int?> GetApproxMemberCounts(IEnumerable<Guid> roomIds);
}
// public interface IVoicePresenceService
// {
//     // Returns true if added; false if duplicate (already present)
//     Task<bool> AddAsync(Guid roomId, ParticipantDto participant, string connectionId);
//     Task RemoveByConnectionAsync(string connectionId);
//     Task RemoveAsync(Guid roomId, Guid userId);
//     Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(Guid roomId);
//     Task<ParticipantDto?> GetAsync(Guid roomId, Guid userId);
//     Task UpdateAsync(Guid roomId, Guid userId, bool? isMuted = null, bool? isVideoOn = null);
//     Task<(Guid? roomId, ParticipantDto? participant)> FindByConnectionAsync(string connectionId);
// }