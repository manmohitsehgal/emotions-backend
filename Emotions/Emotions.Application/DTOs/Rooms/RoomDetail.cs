namespace Emotions.Application.DTOs.Rooms;

public class RoomDetailDto : RoomSummaryDto
{
    public string? Prompt { get; init; }
    public string? Topic { get; init; }
    public string? Description { get; init; }
    public string[] Rules { get; init; } = Array.Empty<string>();
}