namespace Emotions.Application.Pricing;

public record Entitlements(
    bool IsPremium,
    bool CanBookHumanHosted,
    int MonthlyCreditsRemaining,
    int PriorityBoost
);