using CheckDevHealth.Services;
using CheckDevHealth.Services.Analysis;
using Microsoft.UI.Xaml.Controls;

namespace CheckDevHealth.Views;

public sealed partial class AnalysisPage : Page
{
    public AnalysisPage()
    {
        InitializeComponent();
        ModeText.Text = $"Mode: {AppState.Settings.AnalysisMode} (change in Settings)";

        if (AppState.LastResults.Count == 0)
        {
            ResultText.Text = "Run a sweep first (on the Sweep page), then come back here to get AI-generated recommendations.";
            AnalyzeButton.IsEnabled = false;
        }
    }

    private async void AnalyzeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        Progress.IsActive = true;
        ResultText.Text = string.Empty;

        try
        {
            var provider = AnalysisProviderFactory.Create(AppState.Settings);
            ModeText.Text = $"Mode: {AppState.Settings.AnalysisMode} — {provider.DisplayName}" +
                             (provider.RequiresNetwork ? " (sends data to a remote service)" : " (fully offline)");

            var analysis = await provider.AnalyzeAsync(AppState.LastResults, CancellationToken.None);
            ResultText.Text = analysis;
        }
        catch (Exception ex)
        {
            ResultText.Text = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            Progress.IsActive = false;
            AnalyzeButton.IsEnabled = true;
        }
    }
}
