using System.Text.Json.Serialization;

namespace Emotions.Application.DTOs;

public sealed class IdentifyUserRequest
{
    [JsonPropertyName("username")] public string Username { get; set; } = default!;
}