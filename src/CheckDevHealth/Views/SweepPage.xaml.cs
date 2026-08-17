using CheckDevHealth.Models;
using CheckDevHealth.Services;
using Microsoft.UI.Xaml.Controls;

namespace CheckDevHealth.Views;

public sealed partial class SweepPage : Page
{
    private readonly CheckRunner _runner = new();
    private CancellationTokenSource? _cts;

    public SweepPage()
    {
        InitializeComponent();
        if (AppState.LastResults.Count > 0)
        {
            RenderResults(AppState.LastResults);
        }
    }

    private async void RunButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RunButton.IsEnabled = false;
        Progress.IsActive = true;
        SummaryBar.IsOpen = false;
        _cts = new CancellationTokenSource();

        var progress = new Progress<string>(moduleName => StatusText.Text = $"Running: {moduleName}...");

        try
        {
            var results = await _runner.RunAllAsync(progress, _cts.Token);
            AppState.LastResults = results;
            RenderResults(results);
            ShowSummary(results);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Sweep failed: {ex.Message}";
        }
        finally
        {
            Progress.IsActive = false;
            RunButton.IsEnabled = true;
        }
    }

    private void ShowSummary(IReadOnlyList<CheckResult> results)
    {
        var critical = results.Count(r => r.Status is CheckStatus.Critical or CheckStatus.Error);
        var warnings = results.Count(r => r.Status == CheckStatus.Warning);

        StatusText.Text = $"Done. {results.Count} checks, {warnings} warning(s), {critical} critical.";

        SummaryBar.IsOpen = true;
        if (critical > 0)
        {
            SummaryBar.Severity = InfoBarSeverity.Error;
            SummaryBar.Title = "Issues found";
            SummaryBar.Message = $"{critical} critical issue(s) and {warnings} warning(s) found. See details below.";
        }
        else if (warnings > 0)
        {
            SummaryBar.Severity = InfoBarSeverity.Warning;
            SummaryBar.Title = "Some things to review";
            SummaryBar.Message = $"{warnings} warning(s) found. See details below.";
        }
        else
        {
            SummaryBar.Severity = InfoBarSeverity.Success;
            SummaryBar.Title = "Looking good";
            SummaryBar.Message = "No warnings or critical issues found.";
        }
    }

    private void RenderResults(IReadOnlyList<CheckResult> results)
    {
        var groups = results
            .GroupBy(r => r.Category)
            .Select(g => new CategoryGroup
            {
                CategoryName = g.Key,
                Items = g.Select(r => new CheckResultViewModel(r)).ToList()
            })
            .ToList();

        ResultsRepeater.ItemsSource = groups;
    }
}
