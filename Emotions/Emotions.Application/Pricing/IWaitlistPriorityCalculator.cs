namespace Emotions.Application.Pricing;

public interface IWaitlistPriorityCalculator
{
    int CalculatePriority(Guid userId, Entitlements entitlements, WaitlistContext? ctx = null);
}