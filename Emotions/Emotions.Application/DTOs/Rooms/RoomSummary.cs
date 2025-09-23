namespace Emotions.Application.DTOs;

public class RoomSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Theme { get; init; } = "general";
    public bool IsLive { get; init; }
    public int? MemberCount { get; init; }
    public int? MaxParticipants { get; init; }
    public DateTimeOffset? LastActiveAt { get; init; }
    public string Language { get; init; } = "en";
    public string SpeakPolicy { get; init; } = "raiseHand";
    public string? ThumbnailUrl { get; init; }
}