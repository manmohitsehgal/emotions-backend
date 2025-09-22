namespace Emotions.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }

    // Use internal GUIDs consistently across your domain
    public Guid UserId { get; set; }

    // Optional title; keep null if you don't use titles yet
    public string? Title { get; set; }

    // Store encrypted; plaintext is handled in the service layer
    public string TextEncrypted { get; set; } = null!;

    // Exclude from embeddings/recall if true
    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}