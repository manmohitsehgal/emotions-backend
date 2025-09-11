namespace Emotions.Application.DTOs.VoiceRooms;

public class VoiceRoomDetailDto : VoiceRoomSummaryDto
{
    public string? Prompt { get; init; }
    public string? Topic { get; init; }
    public string? Description { get; init; }
    public string[] Rules { get; init; } = Array.Empty<string>();
}