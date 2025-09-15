using System.Security.Claims;
using Emotions.Domain.Entities;

namespace Emotions.Application.Interfaces;

public interface IUserService
{
    Task<User?> TryGetByExternalIdAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<User> ProvisionFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<User> RequireCurrentAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task SetUsernameAsync(ClaimsPrincipal principal, string username, CancellationToken ct = default);
    Task SetInterestsAsync(ClaimsPrincipal principal, IEnumerable<string> slugs, CancellationToken ct = default);
    Task CompleteOnboardingAsync(ClaimsPrincipal principal, bool? analyticsOptIn, CancellationToken ct = default);
    Task<Guid> UpsertFromAuth0Async(string sub, string? email, string? name);
}