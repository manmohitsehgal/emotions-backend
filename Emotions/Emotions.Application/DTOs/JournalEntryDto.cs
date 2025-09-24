namespace Emotions.Application.DTOs;

public class JournalEntryDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = string.Empty; // decrypted
    public string Mode { get; set; } = "text";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Mood { get; set; }
    public string Privacy { get; set; } = "AIEnhanced";
}