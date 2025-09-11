using System.Security.Claims;
using System.Text.RegularExpressions;
using Emotions.Application.Interfaces.Auth;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class OidcProvisioner : IUserProvisioner
{
    private readonly AppDbContext _db;
    public OidcProvisioner(AppDbContext db) => _db = db;

    public async Task<User> GetOrCreateFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        // 1) Stable external id (Auth0 "sub")
        var sub = principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing 'sub' claim from IdP.");

        // 2) If user already exists, return it
        var existing = await _db.Users.SingleOrDefaultAsync(u => u.ExternalId == sub, ct);
        if (existing is not null) return existing;

        // 3) Pull what we can from claims
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        var name = principal.FindFirstValue("name")
                   ?? principal.FindFirstValue(ClaimTypes.Name)
                   ?? (email is not null ? email.Split('@')[0] : null)
                   ?? "User";

        var preferred = principal.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
        var nickname = principal.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value;

        // 4) Build a base username with sensible fallbacks
        var baseUser = preferred
                       ?? nickname
                       ?? (email is not null ? email.Split('@')[0] : null)
                       ?? Slugify(name)
                       ?? Slugify(sub.Replace("|", "-"));

        // Last-ditch fallback (should never be hit)
        if (string.IsNullOrWhiteSpace(baseUser))
            baseUser = "user";

        baseUser = Slugify(baseUser);

        // 5) Ensure DB-unique (append -1, -2, … if needed)
        var username = await EnsureUniqueUsernameAsync(baseUser, ct);

        // 6) Create minimal valid record (fill what your schema requires)
        var user = new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExternalId = sub,
            Email = email,
            Name = name,
            Username = username,
            HasCompletedOnboarding = false,
            // OnboardedAt = null,
            // OnboardingTemplate = null,
            // VoiceRoomId = null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    private async Task<string> EnsureUniqueUsernameAsync(string baseName, CancellationToken ct)
    {
        var name = baseName;
        var i = 0;
        while (await _db.Users.AnyAsync(u => u.Username == name, ct))
        {
            i++;
            name = $"{baseName}-{i}";
        }

        return name;
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var s = input.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^\p{Ll}\p{Lu}\p{Nd}]+", "-"); // non-alnum → hyphen
        s = Regex.Replace(s, @"-+", "-").Trim('-'); // collapse dashes
        return s.Length == 0 ? "" : s;
    }
}

// using System.Security.Claims;
// using Emotions.Application.Interfaces.Auth;
// using Emotions.Domain.Entities;
// using Emotions.Infrastructure.Data;
// using Microsoft.EntityFrameworkCore;
//
// namespace Emotions.Infrastructure.Auth
// {
//     public class OidcProvisioner : IUserProvisioner
//     {
//         private readonly AppDbContext _db;
//
//         public OidcProvisioner(AppDbContext db)
//         {
//             _db = db;
//         }
//
//         public async Task<User> GetOrCreateFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct)
//         {
//             // The stable identifier from Auth0 (the "sub" claim)
//             var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
//                       ?? principal.FindFirstValue("sub");
//
//             if (string.IsNullOrEmpty(sub))
//                 throw new InvalidOperationException("No sub claim found in OIDC token.");
//
//             // Try to find existing user
//             var user = await _db.Users.FirstOrDefaultAsync(u => u.ExternalId == sub, ct);
//             if (user != null) return user;
//
//             // Create new user record
//             user = new User
//             {
//                 Id = Guid.NewGuid(),
//                 ExternalId = sub,
//                 Name = principal.FindFirstValue("name") ?? "Unknown",
//                 Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
//                 CreatedAt = DateTime.UtcNow
//             };
//
//             _db.Users.Add(user);
//             await _db.SaveChangesAsync(ct);
//
//             return user;
//         }
//     }
// }