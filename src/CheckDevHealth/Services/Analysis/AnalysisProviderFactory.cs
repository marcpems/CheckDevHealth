using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Analysis;

/// <summary>
/// Picks the active <see cref="IAnalysisProvider"/> based on <see cref="AppSettings.AnalysisMode"/>.
/// This is the single switch point for moving between cloud and local analysis.
/// </summary>
public static class AnalysisProviderFactory
{
    public static IAnalysisProvider Create(AppSettings settings) => settings.AnalysisMode switch
    {
        AnalysisMode.Local => new LocalAnalysisProvider(),
        AnalysisMode.Cloud or _ => new CloudAnalysisProvider(settings),
    };
}
