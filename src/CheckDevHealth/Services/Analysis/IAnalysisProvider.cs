using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Analysis;

/// <summary>
/// Produces a natural-language summary/recommendation set from the raw sweep results.
/// Implement this to plug in a different AI backend; select which one is active via
/// <see cref="AppSettings.AnalysisMode"/> and <see cref="AnalysisProviderFactory"/>.
/// </summary>
public interface IAnalysisProvider
{
    /// <summary>Human-readable name shown in the UI (e.g. "Cloud (OpenAI-compatible)").</summary>
    string DisplayName { get; }

    /// <summary>True if this provider sends any data off the local machine.</summary>
    bool RequiresNetwork { get; }

    Task<string> AnalyzeAsync(IReadOnlyList<CheckResult> results, CancellationToken cancellationToken);
}
