using System.Text.Json.Serialization;

namespace Emotions.Application.DTOs.Ai
{
    // --- Request sent from C# to Python ---
    public sealed class MindMapSuggestRequestDto
    {
        [JsonPropertyName("journal_text")] public string JournalText { get; set; } = string.Empty;

        [JsonPropertyName("context_nodes")] public List<string> ContextNodes { get; set; } = new();
    }

    // --- Response received from Python, then returned to frontend ---
    public sealed class MindMapEdgeDto
    {
        [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;

        [JsonPropertyName("target")] public string Target { get; set; } = string.Empty;
    }

    public sealed class SuggestionOutDto
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;

        [JsonPropertyName("kind")] public string? Kind { get; set; }
    }

    public sealed class MindMapSuggestResponseDto
    {
        [JsonPropertyName("context_nodes")] public List<string> ContextNodes { get; set; } = new();

        [JsonPropertyName("edges")] public List<MindMapEdgeDto> Edges { get; set; } = new();

        [JsonPropertyName("suggestions")] public List<SuggestionOutDto> Suggestions { get; set; } = new();
    }
}