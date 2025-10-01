using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Emotions.Application.Interfaces.Transcription;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.Services;

public sealed class OpenAISummarizationService : ISummarizationService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAISummarizationService> _logger;
    private readonly AppDbContext _db;

    public OpenAISummarizationService(IHttpClientFactory httpFactory, IConfiguration config,
        ILogger<OpenAISummarizationService> logger, AppDbContext db)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
        _db = db;
    }

    public async Task SummarizeAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var tr = await _db.JournalAttachmentTranscripts
            .FirstOrDefaultAsync(t => t.AttachmentId == attachmentId && t.Status == "Completed", ct);
        if (tr is null)
            throw new InvalidOperationException("Transcript not found or not completed.");

        var model = _config.GetValue<string>("OpenAI:Model") ?? "gpt-4o-mini";
        var apiKey = _config.GetValue<string>("OpenAI:ApiKey") ??
                     throw new InvalidOperationException("OpenAI:ApiKey missing");

        var text = tr.Text ?? string.Empty;
        var payload = new
        {
            model = model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a helpful assistant that writes concise therapy journal summaries and tags."
                },
                new
                {
                    role = "user",
                    content =
                        $"Summarize the following journal transcript in 5-8 bullet points focused on key events, emotions, and any actions. Then provide 5-10 concise tags (single or two-word) as a JSON array labeled 'tags'. Transcript:\n\n{text}"
                }
            },
            temperature = 0.2
        };

        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var reqBody = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await client.PostAsync("https://api.openai.com/v1/chat/completions", reqBody, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content")
            .GetString() ?? "";

        string summary = content;
        string[] tags = Array.Empty<string>();
        try
        {
            var m = Regex.Match(content, @"\[\s*\"".*?\""(?:\s*,\s*\"".*?\"")*\s*\]");
            if (m.Success)
            {
                tags = JsonSerializer.Deserialize<string[]>(m.Value) ?? Array.Empty<string>();
                summary = content.Replace(m.Value, "").Trim();
            }
        }
        catch
        {
        }

        var entity = new TranscriptSummary
        {
            AttachmentId = attachmentId,
            Summary = summary.Trim(),
            Tags = tags,
            Model = model,
            CreatedAt = DateTime.UtcNow
        };

        _db.TranscriptSummaries.Add(entity);
        await _db.SaveChangesAsync(ct);
    }
}