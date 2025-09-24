namespace Emotions.Application.DTOs;

public class UpdateJournalEntryRequestDto
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Mood { get; set; }
    public string? Privacy { get; set; }
}