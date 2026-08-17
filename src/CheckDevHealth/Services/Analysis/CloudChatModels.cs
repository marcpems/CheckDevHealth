using System.Text.Json.Serialization;

namespace CheckDevHealth.Services.Analysis;

/// <summary>Request body for an OpenAI-compatible Chat Completions call.</summary>
public sealed class CloudChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<CloudChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public sealed class CloudChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>Minimal shape of an OpenAI-compatible Chat Completions response, used only if
/// callers want strongly-typed parsing instead of <see cref="System.Text.Json.JsonDocument"/>.</summary>
public sealed class CloudChatResponse
{
    [JsonPropertyName("choices")]
    public List<CloudChatChoice> Choices { get; set; } = new();
}

public sealed class CloudChatChoice
{
    [JsonPropertyName("message")]
    public CloudChatMessage? Message { get; set; }
}
