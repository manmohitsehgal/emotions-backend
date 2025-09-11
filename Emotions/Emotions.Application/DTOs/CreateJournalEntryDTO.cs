namespace Emotions.Application.DTOs;

public class CreateJournalEntryDTO
{
    public string UserId { get; set; } = default!;
    public string Text { get; set; } = default!;
}