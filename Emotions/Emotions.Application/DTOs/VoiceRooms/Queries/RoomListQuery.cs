namespace Emotions.Application.DTOs.VoiceRooms.Queries;

public class RoomListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Theme { get; init; }
    public string? Language { get; init; }
    public string? Status { get; init; } // live|archived|draft
    public string? Q { get; init; }
}