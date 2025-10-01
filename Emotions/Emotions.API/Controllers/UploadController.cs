using Emotions.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Emotions.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController : ControllerBase
{
    private readonly StorageOptions _opt;

    public UploadsController(IOptions<StorageOptions> opt)
    {
        _opt = opt.Value;
    }


    [HttpPost("{*blobKey}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload([FromRoute] string blobKey)
    {
        if (_opt.Provider != "Local") return NotFound();
        if (string.IsNullOrWhiteSpace(blobKey)) return BadRequest("blobKey missing");


        var root = Path.GetFullPath(_opt.LocalRoot);
        var abs = Path.GetFullPath(Path.Combine(root, blobKey));
        if (!abs.StartsWith(root)) return BadRequest("Invalid path");


        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        using var fs = System.IO.File.Create(abs);
        await Request.Body.CopyToAsync(fs);
        return Ok(new { blobKey, size = fs.Length });
    }
}