namespace Emotions.Application.DTOs
{
    public sealed class UserDto
    {
        public required Guid Id { get; init; }
        public string? Username { get; init; }
        public string? Email { get; init; }
        public bool? AnalyticsOptIn { get; init; }
        public bool HasCompletedOnboarding { get; init; }
        public List<string> Interests { get; init; } = new();
    }
}