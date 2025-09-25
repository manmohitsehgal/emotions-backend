namespace Emotions.Application.DTOs;

public record TranscriptResponse(
    Guid AttachmentId,
    string Status,
    string? Language,
    string? Text,
    float? Confidence
);

public record TranscriptionRequest(Guid AttachmentId);