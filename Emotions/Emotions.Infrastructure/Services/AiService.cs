using System.Net.Http.Json;
using Emotions.Application.Interfaces;
using Emotions.Application.Interfaces.AI.Records;

namespace Emotions.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _http;

    public AiService(IHttpClientFactory f)
    {
        _http = f.CreateClient("AiService");
    }

    public async Task<AiTurnResponse> TherapyRespondAsync(AiTurnRequest req, CancellationToken ct)
    {
        var payload = new
        {
            conversation_id = req.ConversationId.ToString(),
            user_text = req.UserText,
            mode = req.Mode,
            journal_context = req.JournalContext
        };
        var r = await _http.PostAsJsonAsync("/therapy/respond", payload, ct);
        r.EnsureSuccessStatusCode();
        var dto = await r.Content.ReadFromJsonAsync<AiTurnResponse>(cancellationToken: ct);
        return dto!;
    }

    public async Task<AiSummaryResponse> TherapySummarizeAsync(AiSummaryRequest req, CancellationToken ct)
    {
        var payload = new
        {
            conversation_id = req.ConversationId.ToString(),
            transcript = req.Transcript.Select(t => new { role = t.role, text = t.text })
        };
        var r = await _http.PostAsJsonAsync("/therapy/summarize", payload, ct);
        r.EnsureSuccessStatusCode();
        var dto = await r.Content.ReadFromJsonAsync<AiSummaryResponse>(cancellationToken: ct);
        return dto!;
    }

    public async Task<SafetyCheckResponse> SafetyCheckAsync(string text, CancellationToken ct)
    {
        var r = await _http.PostAsJsonAsync("/safety/check", new { text }, ct);
        r.EnsureSuccessStatusCode();
        var dto = await r.Content.ReadFromJsonAsync<SafetyCheckResponse>(cancellationToken: ct);
        return dto!;
    }
}