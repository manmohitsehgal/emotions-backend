using Emotions.Application.Pricing;

namespace Emotions.Application.Interfaces;

public interface IPremiumService
{
    Task<Entitlements> GetEntitlementsAsync(Guid userId, CancellationToken ct);
}