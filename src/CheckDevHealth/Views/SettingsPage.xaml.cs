using CheckDevHealth.Models;
using CheckDevHealth.Services;
using Microsoft.UI.Xaml.Controls;

namespace CheckDevHealth.Views;

public sealed partial class SettingsPage : Page
{
    private bool _isLoading = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadFromSettings();
        _isLoading = false;
    }

    private void LoadFromSettings()
    {
        var settings = AppState.Settings;

        ModeRadioButtons.SelectedIndex = settings.AnalysisMode == AnalysisMode.Local ? 1 : 0;
        EndpointBox.Text = settings.CloudEndpoint;
        ModelBox.Text = settings.CloudModel;
        ApiKeyBox.Password = settings.CloudApiKey;
        LocalModelPathBox.Text = settings.LocalModelPath;

        UpdatePanelVisibility();
    }

    private void ModeRadioButtons_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        var isLocal = ModeRadioButtons.SelectedIndex == 1;
        CloudSettingsPanel.Visibility = isLocal ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        LocalSettingsPanel.Visibility = isLocal ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            AnalysisMode = ModeRadioButtons.SelectedIndex == 1 ? AnalysisMode.Local : AnalysisMode.Cloud,
            CloudEndpoint = EndpointBox.Text,
            CloudModel = ModelBox.Text,
            CloudApiKey = ApiKeyBox.Password,
            LocalModelPath = LocalModelPathBox.Text
        };

        AppState.SaveSettings(settings);
        SavedBar.IsOpen = true;
    }
}
