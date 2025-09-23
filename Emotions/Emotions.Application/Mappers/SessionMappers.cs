using Emotions.Application.DTOs.Rooms;
using Emotions.Domain.Entities;

namespace Emotions.Application.Mappers;

public static class SessionMappers
{
    public static SessionDto ToDto(this SupportSession s, int seatsTaken, int waitlistCount)
        => new(
            s.Id, s.Title, s.Description, s.Type, s.Status, s.SpeakPolicy,
            s.StartAt, s.EndAt, s.Capacity, s.AllowListeners,
            s.HostId, s.Host.IsAi, s.Host.DisplayName, s.Host.Credentials,
            s.VoiceRoomId, seatsTaken, waitlistCount);
}