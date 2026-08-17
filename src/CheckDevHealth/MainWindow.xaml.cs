using CheckDevHealth.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CheckDevHealth;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Check Dev Health";
        ContentFrame.Navigate(typeof(SweepPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem { Tag: string tag })
        {
            switch (tag)
            {
                case "sweep":
                    ContentFrame.Navigate(typeof(SweepPage));
                    break;
                case "analysis":
                    ContentFrame.Navigate(typeof(AnalysisPage));
                    break;
                case "defender":
                    ContentFrame.Navigate(typeof(DefenderHotspotPage));
                    break;
            }
        }
    }
}
