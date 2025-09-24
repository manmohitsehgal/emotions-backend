namespace Emotions.Application.DTOs;

public class CreateJournalEntryRequestDto
{
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty; // plain text
    public string? Mood { get; set; }
    public string? Privacy { get; set; } // optional override
}