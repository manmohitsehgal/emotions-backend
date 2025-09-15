using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers.Internal;

[ApiController]
[Route("internal/auth0/provision")]
public class Auth0ProvisionController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IConfiguration _cfg;

    public Auth0ProvisionController(IUserService users, IConfiguration cfg)
    {
        _users = users;
        _cfg = cfg;
    }

    public sealed record ProvisionReq(string sub, string? email, string? name);

    /// <summary>
    /// Called from an Auth0 Action with x-api-key.
    /// Ensures a local user row exists for the given OIDC subject (sub).
    /// Returns the internal user GUID to inject into the access token as a custom claim.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Provision([FromBody] ProvisionReq req,
        [FromHeader(Name = "x-api-key")] string? apiKey)
    {
        var expected = _cfg["INTERNAL_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey != expected)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.sub))
            return BadRequest("Missing sub.");

        // Upsert or find user by external sub/email; return internal GUID as string
        // NOTE: This should be an idempotent "explicit" provision method in your IUserService.
        // If your method is named differently, adjust the call below.
        var userGuid = await _users.UpsertFromAuth0Async(req.sub, req.email, req.name);

        return Ok(new { user_guid = userGuid });
    }
}