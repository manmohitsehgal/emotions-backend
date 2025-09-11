namespace Emotions.Application.DTOs;

public record OnboardingCompleteRequest(
    Guid UserId,
    string Username,
    string? Template,
    bool AnalyticsOptIn
);