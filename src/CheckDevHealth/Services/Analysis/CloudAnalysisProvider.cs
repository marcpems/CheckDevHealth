using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Analysis;

/// <summary>
/// Sends the sweep results to a remote, OpenAI-compatible Chat Completions endpoint
/// (works with OpenAI, Azure OpenAI, or any compatible proxy) and returns the model's analysis.
/// This is the only provider that transmits data off the machine — the endpoint/key/model
/// are all user-configurable in Settings, and switching to <see cref="LocalAnalysisProvider"/>
/// avoids any network call entirely.
/// </summary>
public sealed class CloudAnalysisProvider : IAnalysisProvider
{
    private readonly AppSettings _settings;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    public CloudAnalysisProvider(AppSettings settings)
    {
        _settings = settings;
    }

    public string DisplayName => "Cloud (OpenAI-compatible endpoint)";
    public bool RequiresNetwork => true;

    public async Task<string> AnalyzeAsync(IReadOnlyList<CheckResult> results, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudApiKey))
        {
            return "No API key configured. Go to Settings, choose 'Cloud', and enter an API key for your " +
                   "OpenAI-compatible endpoint (OpenAI, Azure OpenAI, or a self-hosted proxy) to enable AI analysis.";
        }

        var prompt = BuildPrompt(results);

        var requestBody = new CloudChatRequest
        {
            Model = _settings.CloudModel,
            Messages = new List<CloudChatMessage>
            {
                new() { Role = "system", Content = "You are a helpful assistant that reviews Windows developer machine health-check results and gives concise, prioritized, actionable recommendations." },
                new() { Role = "user", Content = prompt }
            },
            Temperature = 0.3
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.CloudEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, Services.AppJsonContext.Default.CloudChatRequest), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.CloudApiKey);

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"AI analysis request failed ({(int)response.StatusCode} {response.ReasonPhrase}):\n{body}";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return content ?? "(empty response)";
        }
        catch (Exception ex)
        {
            return $"Received a response but couldn't parse it: {ex.Message}\n\nRaw response:\n{body}";
        }
    }

    private static string BuildPrompt(IReadOnlyList<CheckResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Here are the results of a Windows developer machine health sweep. " +
                      "Summarize the overall health, then list the top prioritized recommendations to improve " +
                      "performance and developer experience. Group by category, be concise, and skip items that are already fine.");
        sb.AppendLine();

        foreach (var group in results.GroupBy(r => r.Category))
        {
            sb.AppendLine($"## {group.Key}");
            foreach (var r in group)
            {
                sb.AppendLine($"- [{r.Status}] {r.Name}: {r.Value}" + (r.Recommendation is null ? "" : $" (suggestion already noted: {r.Recommendation})"));
            }
        }

        return sb.ToString();
    }
}
