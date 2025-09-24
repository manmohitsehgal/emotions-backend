namespace Emotions.Domain.Entities;

public class JournalPrivacy
{
    public Guid UserId { get; set; }
    public string DefaultPrivacy { get; set; } = "AIEnhanced"; // LocalOnly|AIEnhanced
    public bool DefaultTranscription { get; set; } = true;
    public string? LocalOnlyPinHash { get; set; }
}