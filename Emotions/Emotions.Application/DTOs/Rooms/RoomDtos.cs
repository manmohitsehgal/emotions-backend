namespace Emotions.Application.DTOs.VoiceRooms
{
    public record CreateVoiceRoomRequestDto(string Prompt, int? MaxParticipants);

    public record VoiceRoomDto(Guid Id, string Prompt, int MaxParticipants, DateTime CreatedAt);

    public record JoinRoomRequestDto(Guid RoomId, Guid UserId, string Username);

    public record ToggleMuteRequest(Guid RoomId, Guid UserId, bool IsMuted);

    public record ToggleVideoRequest(Guid RoomId, Guid UserId, bool IsVideoOn);
}