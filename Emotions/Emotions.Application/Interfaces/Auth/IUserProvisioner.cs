using System.Security.Claims;
using Emotions.Domain.Entities;

namespace Emotions.Application.Interfaces.Auth
{
    public interface IUserProvisioner
    {
        Task<User> CreateFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct);
    }
}