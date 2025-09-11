namespace Emotions.Application.DTOs.VoiceRooms;

public class ParticipantDto
{
    public string UserId { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public bool IsMuted { get; init; }
    public bool IsVideoOn { get; init; }
}

public class ParticipantsListDto
{
    public Guid RoomId { get; init; }
    public IReadOnlyList<ParticipantDto> Participants { get; init; } = Array.Empty<ParticipantDto>();
}