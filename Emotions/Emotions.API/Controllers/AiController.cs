using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        [HttpGet("prompt")]
        public IActionResult Prompt()
        {
            // Simple time-of-day prompt to avoid 404s and support production usage without external AI deps.
            var hour = DateTime.UtcNow.Hour;
            string text = hour < 12
                ? "Morning reflection: What’s one intention for today?"
                : hour < 18
                    ? "Afternoon check‑in: What’s gone better than expected?"
                    : "Evening wind‑down: What’s one thing you appreciated today?";
            return Ok(new { id = Guid.NewGuid(), text });
        }
    }
}