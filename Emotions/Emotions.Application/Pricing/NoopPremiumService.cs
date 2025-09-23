using Emotions.Application.Interfaces;

namespace Emotions.Application.Pricing;

public class NoopPremiumService : IPremiumService
{
    public Task<Entitlements> GetEntitlementsAsync(Guid userId, CancellationToken ct)
        => Task.FromResult(new Entitlements(
            IsPremium: false,
            CanBookHumanHosted: true, // keep open for now
            MonthlyCreditsRemaining: int.MaxValue,
            PriorityBoost: 0));
}