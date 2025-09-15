using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.Auth;
using Emotions.Infrastructure.Auth;
using Emotions.Infrastructure.Data;
using Emotions.Infrastructure.Services;
using Emotions.Infrastructure.SignalR;
using Emotions.Infrastructure.SignalR.Presence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services (before Build) ----------

// Controllers + consistent JSON
builder.Services.AddControllers();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Domain services
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IVoiceRoomService, VoiceRoomService>();

// Explicit OIDC provisioner (used only when you *choose* to provision)
builder.Services.AddScoped<IUserProvisioner, OidcProvisioner>();

// ---------- Auth0 (OIDC) ----------
var auth0Domain = builder.Configuration["Auth0:Domain"]; // e.g. dev-xxxx.us.auth0.com
var auth0Audience = builder.Configuration["Auth0:Audience"]; // e.g. emotions0api
if (string.IsNullOrWhiteSpace(auth0Domain) || string.IsNullOrWhiteSpace(auth0Audience))
    throw new InvalidOperationException("Auth0:Domain and Auth0:Audience must be configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;

        // Harden defaults
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Your Auth0 Action injects this custom GUID claim
            NameClaimType = "https://emotions.app/user_guid",

            ClockSkew = TimeSpan.FromSeconds(45),
            RoleClaimType = "roles",
            ValidTypes = new[] { "at+jwt", "JWT" }
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // 1) Prefer Authorization header (safe parse)
                if (ctx.Request.Headers.TryGetValue(HeaderNames.Authorization, out var values))
                {
                    var raw = values.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(raw) &&
                        AuthenticationHeaderValue.TryParse(raw, out var ahv) &&
                        ahv.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(ahv.Parameter))
                    {
                        ctx.Token = ahv.Parameter;
                    }
                }

                // 2) Allow SignalR query token only for the voice hub
                if (string.IsNullOrEmpty(ctx.Token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hub/voice"))
                {
                    var q = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrWhiteSpace(q))
                        ctx.Token = q.ToString();
                }

                // Optional debug (safe; no token content)
                if (!string.IsNullOrEmpty(ctx.Token))
                {
                    var segs = ctx.Token.Split('.').Length;
                    Console.WriteLine($"[Auth0] OnMessageReceived len={ctx.Token.Length} segs={segs}");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("[Auth0] Auth failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },

            OnTokenValidated = ctx =>
            {
                const string Key = "https://emotions.app/user_guid";
                var guid = ctx.Principal?.FindFirst(Key)?.Value;

                if (string.IsNullOrWhiteSpace(guid) || !Guid.TryParse(guid, out _))
                {
                    ctx.Fail("Missing or invalid user GUID claim.");
                    return Task.CompletedTask;
                }

                var id = (ClaimsIdentity)ctx.Principal!.Identity!;
                if (id.FindFirst(ClaimTypes.NameIdentifier) is null)
                    id.AddClaim(new Claim(ClaimTypes.NameIdentifier, guid));
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// CORS (dev/Expo)
const string CorsPolicy = "AllowExpo";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(
                "http://localhost:19006",
                "http://127.0.0.1:19006",
                "http://localhost:8081",
                "http://127.0.0.1:8081"
            );
    });
});

// Swagger + Bearer Auth button
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Emotions API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter Auth0 Access Token: Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { scheme, Array.Empty<string>() }
    });
});

builder.Services.AddHttpContextAccessor();

// HttpClient for Python AI service (proxy)
builder.Services.AddHttpClient("AiService", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["AiService:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddSingleton<IVoicePresenceService>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("Redis") ?? cfg["REDIS_URL"];
    var prefix = cfg["PRESENCE_KEY_PREFIX"] ?? "dev"; // dev|qa|prod

    if (string.IsNullOrWhiteSpace(conn))
        throw new InvalidOperationException("Redis presence requires REDIS_URL/ConnectionString");

    return new RedisVoicePresenceService(conn, prefix); // one implementation
});

var app = builder.Build();

// ---------- Middleware / Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // enable in prod

app.UseRouting();
app.UseCors(CorsPolicy);

// (optional request logs)
app.Use(async (ctx, next) =>
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrEmpty(auth))
        Console.WriteLine(
            $"[REQ] {ctx.Request.Method} {ctx.Request.Path} authHeader={(auth.Length > 14 ? auth[..14] + "…" : auth)}");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ❌ REMOVED: implicit auto-provision on every request
// This was recreating “ghost users”.
// DO NOT auto-provision from middleware.
// If you need to provision, call an explicit endpoint instead.

app.MapControllers();
app.MapHub<VoiceHub>("/hub/voice");

// Optional: bind explicit dev URL/port
app.Urls.Add("http://0.0.0.0:5137");

app.Run();