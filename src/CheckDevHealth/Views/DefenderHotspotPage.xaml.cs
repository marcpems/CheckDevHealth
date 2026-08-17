using CheckDevHealth.Models;
using CheckDevHealth.Services.DefenderHotspot;
using Microsoft.UI.Xaml.Controls;

namespace CheckDevHealth.Views;

public sealed partial class DefenderHotspotPage : Page
{
    private readonly DefenderHotspotService _service = new();
    private CancellationTokenSource? _cts;

    public DefenderHotspotPage()
    {
        InitializeComponent();
        ElevationBar.IsOpen = !DefenderHotspotService.IsElevated();
    }

    private async void RecordButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!DefenderHotspotService.IsElevated())
        {
            ElevationBar.IsOpen = true;
            return;
        }

        var seconds = (DurationCombo.SelectedItem as ComboBoxItem)?.Tag is string tag && int.TryParse(tag, out var s) ? s : 30;

        RecordButton.IsEnabled = false;
        DurationCombo.IsEnabled = false;
        Progress.IsActive = true;
        SummaryBar.IsOpen = false;
        PathsSection.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        ProcessesSection.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        _cts = new CancellationTokenSource();

        var progress = new Progress<string>(msg => StatusText.Text = msg);

        try
        {
            var report = await _service.RecordAndAnalyzeAsync(seconds, progress, _cts.Token);
            RenderReport(report);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Failed.";
            SummaryBar.IsOpen = true;
            SummaryBar.Severity = InfoBarSeverity.Error;
            SummaryBar.Title = "Recording failed";
            SummaryBar.Message = ex.Message;
        }
        finally
        {
            Progress.IsActive = false;
            RecordButton.IsEnabled = true;
            DurationCombo.IsEnabled = true;
        }
    }

    private void RenderReport(DefenderHotspotReport report)
    {
        StatusText.Text = $"Done. {report.RealTimeScanCount} real-time scans, {report.TotalRealTimeScanDurationMs / 1000.0:N2}s total scan time.";

        var devPathCount = report.Paths.Count(p => p.IsDevRelated && !p.AlreadyExcluded);
        var devProcessCount = report.Processes.Count(p => p.IsDevRelated && !p.AlreadyExcluded);
        var candidateCount = devPathCount + devProcessCount;

        SummaryBar.IsOpen = true;
        if (candidateCount > 0)
        {
            SummaryBar.Severity = InfoBarSeverity.Warning;
            SummaryBar.Title = "Exclusion candidates found";
            SummaryBar.Message = $"{candidateCount} dev-related hotspot(s) are not yet excluded from Defender real-time scanning. Review below and exclude the ones you recognize.";
        }
        else
        {
            SummaryBar.Severity = InfoBarSeverity.Success;
            SummaryBar.Title = "No obvious dev-related hotspots";
            SummaryBar.Message = "Nothing recorded looked like an un-excluded dev tool cache or build folder. Try recording during a heavier workload (full rebuild, package restore) for a more complete picture.";
        }

        if (report.Paths.Count > 0)
        {
            PathsSection.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            PathsRepeater.ItemsSource = report.Paths.Select(p => new DefenderPathHotspotViewModel(p)).ToList();
        }

        if (report.Processes.Count > 0)
        {
            ProcessesSection.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            ProcessesRepeater.ItemsSource = report.Processes.Select(p => new DefenderProcessHotspotViewModel(p)).ToList();
        }
    }

    private async void ExcludePathButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path })
        {
            return;
        }

        var confirmed = await ConfirmAsync(
            "Add Defender Exclusion",
            $"This will run Add-MpPreference -ExclusionPath and prompt for administrator approval (UAC). Exclude this folder from real-time scanning?\n\n{path}");

        if (!confirmed)
        {
            return;
        }

        var ok = await DefenderHotspotService.AddExclusionPathAsync(path);
        StatusText.Text = ok ? $"Excluded: {path}" : "Exclusion was not applied (declined or failed).";
    }

    private async void ExcludeProcessButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string processPath })
        {
            return;
        }

        var confirmed = await ConfirmAsync(
            "Add Defender Exclusion",
            $"This will run Add-MpPreference -ExclusionProcess and prompt for administrator approval (UAC). Exclude this process from real-time scanning?\n\n{processPath}");

        if (!confirmed)
        {
            return;
        }

        var ok = await DefenderHotspotService.AddExclusionProcessAsync(processPath);
        StatusText.Text = ok ? $"Excluded: {processPath}" : "Exclusion was not applied (declined or failed).";
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Exclude",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
