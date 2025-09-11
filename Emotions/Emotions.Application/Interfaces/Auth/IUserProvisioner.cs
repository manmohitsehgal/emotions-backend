using System.Security.Claims;
using Emotions.Domain.Entities;

namespace Emotions.Application.Interfaces.Auth
{
    public interface IUserProvisioner
    {
        Task<User> GetOrCreateFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct);
    }
}