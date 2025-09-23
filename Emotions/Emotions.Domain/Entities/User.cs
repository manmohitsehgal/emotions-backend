using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emotions.Domain.Entities;

public class User
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable external identity key from OIDC (e.g. Auth0 "sub" claim).
    /// Example: "auth0|123456789".
    /// </summary>
    [Required]
    public string ExternalId { get; set; } = default!;

    /// <summary>
    /// App-specific username chosen during onboarding (e.g. "mindful_mani").
    /// Unique inside the app.
    /// </summary>
    [Required]
    public string Username { get; set; } = default!;

    /// <summary>
    /// Display name (from claims "name", can be updated later).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Email address (from claims if available).
    /// </summary>
    public string? Email { get; set; }

    public bool IsMuted { get; set; }

    // 🔧 Voice room relation (optional)
    [ForeignKey("VoiceRoom")] public Guid? VoiceRoomId { get; set; }
    public Room? VoiceRoom { get; set; }

    public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();

    // Onboarding metadata
    public bool HasCompletedOnboarding { get; set; }
    public string? OnboardingTemplate { get; set; }
    public bool AnalyticsOptIn { get; set; }
    public DateTime? OnboardedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}