using System.Security.Claims;
using System.Text.RegularExpressions;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Auth;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IUserProvisioner _provisioner;

    public UserService(AppDbContext db, IUserProvisioner provisioner)
    {
        _db = db;
        _provisioner = provisioner;
    }

    public async Task<User?> TryGetByExternalIdAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(sub)) return null;

        return Guid.TryParse(sub, out var guid)
            ? await _db.Users.FindAsync(new object[] { guid }, ct)
            : await _db.Users.SingleOrDefaultAsync(u => u.ExternalId == sub, ct);
    }

    public async Task<User> ProvisionFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var existing = await TryGetByExternalIdAsync(principal, ct);
        if (existing is not null) return existing;

        try
        {
            return await _provisioner.CreateFromClaimsAsync(principal, ct); // create-only
        }
        catch (DbUpdateException)
        {
            var after = await TryGetByExternalIdAsync(principal, ct);
            if (after is not null) return after;
            throw;
        }
    }

    public async Task<User> RequireCurrentAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        return await TryGetByExternalIdAsync(principal, ct)
               ?? throw new KeyNotFoundException("User not found.");
    }

    public async Task SetUsernameAsync(ClaimsPrincipal principal, string username, CancellationToken ct = default)
    {
        var user = await RequireCurrentAsync(principal, ct);
        var final = Slugify(username);

        // ensure unique inside app
        if (await _db.Users.AnyAsync(u => u.Username == final && u.Id != user.Id, ct))
            final = await EnsureUniqueUsernameAsync(final, ct);

        user.Username = final;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetInterestsAsync(ClaimsPrincipal principal, IEnumerable<string> slugs,
        CancellationToken ct = default)
    {
        var user = await RequireCurrentAsync(principal, ct);
        await _db.Entry(user).Collection(u => u.UserInterests).LoadAsync(ct);

        var normalized = slugs
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        var interests = await _db.Interests
            .Where(i => normalized.Contains(i.Slug))
            .ToListAsync(ct);

        // Optional: auto-create missing slugs
        var missing = normalized.Except(interests.Select(i => i.Slug)).ToList();
        foreach (var ms in missing)
            interests.Add(new Interest { Slug = ms, Name = ms });

        var desiredIds = interests.Select(i => i.Id).ToHashSet();
        var currentIds = user.UserInterests.Select(ui => ui.InterestId).ToHashSet();

        foreach (var ui in user.UserInterests.Where(ui => !desiredIds.Contains(ui.InterestId)).ToList())
            _db.Remove(ui);

        foreach (var id in desiredIds.Except(currentIds))
            user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = id });

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteOnboardingAsync(ClaimsPrincipal principal, bool? analyticsOptIn,
        CancellationToken ct = default)
    {
        var user = await RequireCurrentAsync(principal, ct);

        if (analyticsOptIn is not null)
            user.AnalyticsOptIn = analyticsOptIn.Value;

        user.HasCompletedOnboarding = true;
        user.OnboardedAt ??= DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // -------- helpers --------

    public async Task<Guid> UpsertFromAuth0Async(string sub, string? email, string? name)
    {
        if (string.IsNullOrWhiteSpace(sub))
            throw new ArgumentException("sub is required", nameof(sub));

        var now = DateTime.UtcNow;

        // 1) Try by external subject (Auth0 "sub")
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ExternalId == sub);

        // 2) Optional fallback by email (only if you treat email as unique-ish)
        if (user is null && !string.IsNullOrWhiteSpace(email))
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = sub, // UNIQUE index recommended
                Email = email,
                Name = name,
                Username = await GenerateUniqueUsernameAsync(email, name),
                HasCompletedOnboarding = false,
                CreatedAt = now,
            };
            _db.Users.Add(user);
        }
        else
        {
            // Light refresh of known fields
            if (string.IsNullOrWhiteSpace(user.ExternalId)) user.ExternalId = sub;
            if (!string.IsNullOrWhiteSpace(email)) user.Email = email;
            if (!string.IsNullOrWhiteSpace(name)) user.Name = name;
        }

        await _db.SaveChangesAsync();
        return user.Id;
    }

    public bool TryGetAuthenticatedUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? principal.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var parsed))
        {
            userId = parsed;
            return true;
        }

        userId = default;
        return false;
    }

    public Guid GetAuthenticatedUserId(ClaimsPrincipal principal)
    {
        if (TryGetAuthenticatedUserId(principal, out var guid))
            return guid;

        throw new InvalidOperationException("User id claim is missing or not a valid GUID.");
    }

    private async Task<string> GenerateUniqueUsernameAsync(string? email, string? name, CancellationToken ct = default)
    {
        // 1) Derive a base candidate
        var baseUser = DeriveBaseUsername(email, name);

        // 2) Slugify + clamp to leave room for numeric suffixes later
        baseUser = Slugify(baseUser);
        if (string.IsNullOrWhiteSpace(baseUser)) baseUser = "user";

        // You enforce max 128 chars in the model; keep head short enough to append suffixes.
        const int MaxLen = 128;
        if (baseUser.Length > MaxLen) baseUser = baseUser[..MaxLen];

        // 3) Make it unique against the DB
        return await EnsureUniqueUsernameAsync(baseUser, ct);
    }

// Build a reasonable base from email/name
    private static string DeriveBaseUsername(string? email, string? name)
    {
        // email local part first (most stable/expected)
        var local = (email ?? "").Split('@')[0];
        if (!string.IsNullOrWhiteSpace(local)) return local;

        if (!string.IsNullOrWhiteSpace(name)) return name;

        return "user";
    }

// Slugify to [a-z0-9-_.], collapse spaces/invalids to hyphens
    private static string Slugify(string value)
    {
        var s = (value ?? string.Empty).Trim().ToLowerInvariant();

        // Replace whitespace with hyphens
        s = Regex.Replace(s, @"\s+", "-");

        // Remove disallowed chars (keep letters, digits, -, _, .)
        s = Regex.Replace(s, @"[^a-z0-9._-]", "");

        // Collapse multiple hyphens and trim
        s = Regex.Replace(s, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(s) ? "user" : s;
    }

// Append -2, -3, ... until unique; keep under 128 chars including suffix
    private async Task<string> EnsureUniqueUsernameAsync(string baseUser, CancellationToken ct)
    {
        const int MaxLen = 128;
        var candidate = baseUser;
        var n = 1;

        while (await _db.Users.AnyAsync(u => u.Username == candidate, ct))
        {
            n++;
            var suffix = "-" + n.ToString();
            var headLen = Math.Max(1, MaxLen - suffix.Length);
            var head = baseUser.Length > headLen ? baseUser[..headLen] : baseUser;
            candidate = head + suffix;
        }

        return candidate;
    }
}