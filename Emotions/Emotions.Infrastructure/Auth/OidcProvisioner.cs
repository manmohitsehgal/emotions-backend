using System.Security.Claims;
using System.Text.RegularExpressions;
using Emotions.Application.Interfaces.Auth;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Auth
{
    /// <summary>
    /// Create-only provisioner: turns OIDC claims into a new User row.
    /// Caller (UserService) is responsible for checking existence and
    /// catching unique constraint races on Users.ExternalId.
    /// </summary>
    public class OidcProvisioner : IUserProvisioner
    {
        private readonly AppDbContext _db;
        public OidcProvisioner(AppDbContext db) => _db = db;

        private const int MaxUsernameLength = 128;

        public async Task<User> CreateFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct = default)
        {
            var sub = principal.FindFirstValue("sub")
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? throw new InvalidOperationException("Missing 'sub' claim.");

            var email = principal.FindFirstValue(ClaimTypes.Email)
                        ?? principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            var name = principal.FindFirstValue("name")
                       ?? principal.FindFirstValue(ClaimTypes.Name)
                       ?? (email is not null ? email.Split('@')[0] : null)
                       ?? "User";

            var preferred = principal.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            var nickname = principal.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value;

            // Derive a base username with sensible fallbacks
            var baseUser = preferred
                           ?? nickname
                           ?? (email is not null ? email.Split('@')[0] : null)
                           ?? Slugify(name)
                           ?? Slugify(sub.Replace("|", "-"))
                           ?? "user";

            baseUser = Slugify(baseUser);
            if (string.IsNullOrWhiteSpace(baseUser)) baseUser = "user";

            // Ensure DB-unique (append -2, -3, …; keep under 128 chars)
            var username = await EnsureUniqueUsernameAsync(baseUser, ct);

            var user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = sub, // UNIQUE index recommended
                Username = username,
                Name = name,
                Email = email,
                HasCompletedOnboarding = false,
                AnalyticsOptIn = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        // ---------- helpers ----------

        // keep lowercase letters, digits, dot, underscore, hyphen
        private static string Slugify(string value)
        {
            var s = (value ?? string.Empty).Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-"); // spaces -> hyphen
            s = Regex.Replace(s, @"[^a-z0-9._-]", ""); // strip others
            s = Regex.Replace(s, @"-+", "-").Trim('-'); // collapse hyphens
            return string.IsNullOrWhiteSpace(s) ? "user" : s;
        }

        private async Task<string> EnsureUniqueUsernameAsync(string baseUser, CancellationToken ct)
        {
            // clamp base so we have room for suffixes
            var head = baseUser.Length > MaxUsernameLength ? baseUser[..MaxUsernameLength] : baseUser;
            var candidate = head;
            var n = 1;

            while (await _db.Users.AnyAsync(u => u.Username == candidate, ct))
            {
                n++;
                var suffix = "-" + n;
                var headLen = Math.Max(1, MaxUsernameLength - suffix.Length);
                var trimmedHead = head.Length > headLen ? head[..headLen] : head;
                candidate = trimmedHead + suffix;
            }

            return candidate;
        }
    }
}