using System.Security.Claims;
using System.Text.RegularExpressions;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Auth;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Emotions.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserProvisioner _provisioner;

    public UserService(AppDbContext db, IHttpContextAccessor httpContextAccessor, IUserProvisioner provisioner)
    {
        _dbContext = db;
        _httpContextAccessor = httpContextAccessor;
        _provisioner = provisioner;
    }

    public async Task<User> GetOrCreateAsync(CancellationToken ct = default)
    {
        var principal = GetPrincipalOrThrow();

        // OIDC (Auth0) path: use 'sub' as stable external id (e.g., "auth0|abc123")
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(sub) && !Guid.TryParse(sub, out _))
        {
            // Look up by ExternalId; create via provisioner if first-time login
            var existing = await _dbContext.Users.SingleOrDefaultAsync(u => u.ExternalId == sub, ct);
            if (existing is not null) return existing;

            var created = await _provisioner.GetOrCreateFromClaimsAsync(principal, ct);
            return created;
        }

        // Legacy GUID path (older self-hosted tokens carried a GUID in a claim)
        var guid = TryGetGuidFromClaims(principal)
                   ?? throw new InvalidOperationException("User id claim is not a valid GUID or OIDC subject.");

        var user = await _dbContext.Users.FindAsync([guid], ct);
        if (user is not null) return user;

        // Last-resort: create a minimal valid record for legacy GUID tokens
        // Ensure Username is non-null and unique-ish
        var username = $"user-{guid.ToString("N")[..8]}";
        user = new User
        {
            Id = guid,
            ExternalId = guid.ToString(), // keeps a stable external reference for legacy
            Username = username, // satisfies NOT NULL
            CreatedAt = DateTime.UtcNow, // if your schema has a default, this is still fine
            HasCompletedOnboarding = false
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> IdentifyAsync(string username, CancellationToken ct = default)
    {
        var user = await GetOrCreateAsync(ct);
        user.Username = Slugify(username);
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> CompleteOnboardingAsync(bool analyticsOptIn, CancellationToken ct = default)
    {
        var user = await GetOrCreateAsync(ct);
        user.AnalyticsOptIn = analyticsOptIn;
        user.HasCompletedOnboarding = true;
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }

    public Task<User> GetCurrentAsync(CancellationToken ct = default)
        => GetOrCreateAsync(ct);

    // ---------- helpers ----------

    private ClaimsPrincipal GetPrincipalOrThrow()
    {
        var p = _httpContextAccessor.HttpContext?.User;
        if (p?.Identity == null || !p.Identity.IsAuthenticated)
            throw new UnauthorizedAccessException("No authenticated user context.");
        return p;
    }

    private static Guid? TryGetGuidFromClaims(ClaimsPrincipal principal)
    {
        var candidates = new[]
        {
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub),
            principal.FindFirst("uid")?.Value
        };

        foreach (var s in candidates)
        {
            if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out var g))
                return g;
        }

        return null;
    }

    public async Task<Guid> UpsertFromAuth0Async(string sub, string? email, string? name)
    {
        var now = DateTime.UtcNow;

        // 1) Try by sub (fast path)
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.ExternalId == sub);

        // 2) Fallback by email (if you treat email as unique identity)
        if (user is null && !string.IsNullOrWhiteSpace(email))
            user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = sub,
                Email = email,
                Name = name,
                Username = await GenerateUniqueUsernameAsync(email, name), // see helper below
                HasCompletedOnboarding = false,
                CreatedAt = now,
            };
            _dbContext.Users.Add(user);
        }
        else
        {
            // Refresh fields without nuking non-null values
            if (string.IsNullOrWhiteSpace(user.ExternalId)) user.ExternalId = sub;
            if (!string.IsNullOrWhiteSpace(email)) user.Email = email;
            if (!string.IsNullOrWhiteSpace(name)) user.Name = name;
        }

        await _dbContext.SaveChangesAsync();
        return user.Id;
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "user";
        var s = input.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^\p{Ll}\p{Lu}\p{Nd}]+", "-");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(s) ? "user" : s;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string? email, string? name)
    {
        // seed from email prefix or name; fallback to short guid
        string baseSlug =
            (!string.IsNullOrWhiteSpace(email) ? email.Split('@')[0] :
                !string.IsNullOrWhiteSpace(name) ? name.Replace(" ", "").ToLowerInvariant() :
                "user") ?? "user";

        baseSlug = new string(baseSlug.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "user";

        string candidate = baseSlug;
        int i = 0;
        while (await _dbContext.Users.AnyAsync(u => u.Username == candidate))
        {
            i++;
            candidate = $"{baseSlug}{i}";
            if (i > 50)
            {
                candidate = $"user_{Guid.NewGuid():N}".Substring(0, 16);
                break;
            }
        }

        return candidate;
    }
}