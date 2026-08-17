using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Analysis;

/// <summary>
/// Placeholder for fully on-device analysis (no network calls at all).
///
/// TODO (future work): wire this up to a local inference runtime, e.g.:
///   - Windows AI Foundry / "Phi Silica" APIs (built into Windows Copilot+ PCs), or
///   - ONNX Runtime GenAI with a local ONNX model, or
///   - A local Ollama/llama.cpp server reachable only on localhost.
///
/// Once implemented, this provider should not make any external HTTP calls — that's what
/// distinguishes it from <see cref="CloudAnalysisProvider"/> and is what lets users choose
/// "Local" in Settings for a fully offline/private analysis.
/// </summary>
public sealed class LocalAnalysisProvider : IAnalysisProvider
{
    public string DisplayName => "Local (on-device, offline) — not implemented yet";
    public bool RequiresNetwork => false;

    public Task<string> AnalyzeAsync(IReadOnlyList<CheckResult> results, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            "Local, offline AI analysis isn't implemented yet in this build. " +
            "Switch to 'Cloud' in Settings to get AI-generated recommendations, or read the " +
            "per-check recommendations shown in the results list — those never require any AI or network call.");
    }
}
