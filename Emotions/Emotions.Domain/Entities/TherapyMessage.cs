using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class TherapyMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public TherapyConversation Conversation { get; set; } = default!;
    public AuthorType AuthorType { get; set; }
    public string TextEncrypted { get; set; } = default!; // encrypt/decrypt in service
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Model { get; set; }
    public int? TokensIn { get; set; }
    public int? TokensOut { get; set; }
    public bool IsPrivate { get; set; }
}