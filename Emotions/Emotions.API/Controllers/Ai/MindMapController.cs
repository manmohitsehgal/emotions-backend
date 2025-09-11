using System.Net;
using Emotions.Application.DTOs.Ai;
using Microsoft.AspNetCore.Mvc;

namespace Emotions.API.Controllers.Ai
{
    [ApiController]
    [Route("api/ai/mindmap")]
    public sealed class MindMapAiController : ControllerBase
    {
        private readonly HttpClient _ai;

        public MindMapAiController(IHttpClientFactory factory)
        {
            _ai = factory.CreateClient("AiService");
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> Suggest([FromBody] MindMapSuggestRequestDto req, CancellationToken ct)
        {
            // Forward to Python FastAPI: POST /mindmap/suggest
            using var resp = await _ai.PostAsJsonAsync("/mindmap/suggest", req, ct);

            // Bubble up error body if Python returns an error
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                return StatusCode((int)resp.StatusCode, new
                {
                    error = "AI service error",
                    status = (int)resp.StatusCode,
                    body
                });
            }

            var result = await resp.Content.ReadFromJsonAsync<MindMapSuggestResponseDto>(cancellationToken: ct);
            if (result is null)
            {
                return StatusCode((int)HttpStatusCode.BadGateway, new
                {
                    error = "Invalid AI response",
                });
            }

            return Ok(result);
        }
    }
}