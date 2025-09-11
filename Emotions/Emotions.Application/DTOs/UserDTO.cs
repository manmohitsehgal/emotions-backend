namespace Emotions.Application.DTOs
{
    public sealed class UserDto
    {
        public Guid Id { get; init; }
        public string? Username { get; init; }
        public bool HasCompletedOnboarding { get; init; }
        public bool AnalyticsOptIn { get; init; }
    }
}