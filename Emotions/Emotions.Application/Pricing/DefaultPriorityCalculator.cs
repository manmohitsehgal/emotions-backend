namespace Emotions.Application.Pricing;

public class DefaultPriorityCalculator : IWaitlistPriorityCalculator
{
    public int CalculatePriority(Guid userId, Entitlements entitlements, WaitlistContext? ctx = null)
        => (entitlements.IsPremium ? 100 : 0) + entitlements.PriorityBoost;
}