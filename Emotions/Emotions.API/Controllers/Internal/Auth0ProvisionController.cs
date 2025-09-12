using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers.Internal;

[ApiController]
[Route("internal/auth0/provision")]
public class Auth0ProvisionController : ControllerBase
{
    private readonly IUserService _users; // your user service
    private readonly IConfiguration _cfg;

    public Auth0ProvisionController(IUserService users, IConfiguration cfg)
    {
        _users = users;
        _cfg = cfg;
    }

    public record ProvisionReq(string sub, string? email, string? name);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Provision([FromBody] ProvisionReq req,
        [FromHeader(Name = "x-api-key")] string? apiKey)
    {
        var expected = _cfg["INTERNAL_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey != expected)
            return Unauthorized();

        // Upsert or find user by external sub/email; return internal GUID
        var userGuid = await _users.UpsertFromAuth0Async(req.sub, req.email, req.name);
        return Ok(new { user_guid = userGuid }); // string GUID
    }
}