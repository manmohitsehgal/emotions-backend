using Emotions.Application.DTOs.Rooms;

namespace Emotions.Infrastructure.SignalR
{
    public interface IVoiceClient
    {
        Task HandRaised(object payload); // { roomId, userId }
        Task MicGranted(object payload); // { roomId, userId }
        Task UserKicked(object payload); // { roomId, userId, reason }
        Task UserBanned(object payload); // { roomId, userId, minutes, reason }
        Task ToolcardDropped(object payload); // { roomId, type, payload }

        Task MemberJoined(object payload); // { roomId, userId, displayName }
        Task MemberLeft(object payload); // { roomId, userId }
        Task MemberCountUpdated(object payload); // { roomId, count }
        Task RoomRoster(IReadOnlyList<ParticipantDto> roster);
        Task MuteChanged(object payload); // { roomId, userId, isMuted }

        // --- new session lifecycle + booking events for Rooms 2.0 ---
        Task SessionPublished(Guid sessionId, DateTimeOffset startAt);
        Task SeatingUpdated(Guid sessionId, int seatsTaken, int waitlistCount);
        Task BookingPromoted(Guid sessionId);
        Task SessionLive(Guid sessionId, Guid voiceRoomId);
        Task SessionCompleted(Guid sessionId);
    }
}