namespace Emotions.Application.DTOs;

public record JournalEntryDto(
    Guid Id,
    Guid UserId,
    string? Title,
    string Text, // decrypted
    bool IsPrivate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);