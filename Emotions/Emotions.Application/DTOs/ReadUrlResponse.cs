namespace Emotions.Application.DTOs;

public record ReadUrlResponse(string url, DateTimeOffset expiresAt);