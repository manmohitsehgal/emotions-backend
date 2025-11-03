using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities;

public class UserSubscription
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public bool Premium { get; set; }
    public bool FreeTrialActive { get; set; }
    public DateTime? FreeTrialEndsAt { get; set; }
}