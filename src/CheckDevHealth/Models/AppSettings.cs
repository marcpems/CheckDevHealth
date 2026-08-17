namespace CheckDevHealth.Models;

/// <summary>
/// Which engine produces the natural-language analysis of the sweep results.
/// </summary>
public enum AnalysisMode
{
    /// <summary>Calls out to a remote/cloud AI service (e.g. an OpenAI-compatible chat completion endpoint).</summary>
    Cloud,

    /// <summary>Runs inference entirely on-device with no network calls. Not implemented yet.</summary>
    Local
}

/// <summary>
/// User-configurable settings, persisted to %LOCALAPPDATA%\CheckDevHealth\settings.json.
/// </summary>
public sealed class AppSettings
{
    public AnalysisMode AnalysisMode { get; set; } = AnalysisMode.Cloud;

    // --- Cloud provider settings ---
    // Any OpenAI-compatible Chat Completions endpoint (Azure OpenAI, OpenAI, local proxy, etc).
    public string CloudEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string CloudModel { get; set; } = "gpt-4o-mini";
    public string CloudApiKey { get; set; } = string.Empty;

    // --- Local provider settings (reserved for future use) ---
    // e.g. path to a local ONNX/GGUF model when local inference is implemented.
    public string LocalModelPath { get; set; } = string.Empty;
}
