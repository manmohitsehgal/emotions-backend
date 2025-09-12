using Emotions.Domain.Entities;

namespace Emotions.Application.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateAsync(CancellationToken ct = default);
    Task<User> IdentifyAsync(string username, CancellationToken ct = default);
    Task<User> CompleteOnboardingAsync(bool analyticsOptIn, CancellationToken ct = default);
    Task<User> GetCurrentAsync(CancellationToken ct = default);
    Task<Guid> UpsertFromAuth0Async(string sub, string? email, string? name);
}