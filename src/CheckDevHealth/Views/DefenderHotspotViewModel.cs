using CheckDevHealth.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CheckDevHealth.Views;

/// <summary>UI-friendly wrapper around a path hotspot for x:Bind, with an "Exclude" action.</summary>
public sealed class DefenderPathHotspotViewModel
{
    private readonly DefenderPathHotspot _hotspot;

    public DefenderPathHotspotViewModel(DefenderPathHotspot hotspot)
    {
        _hotspot = hotspot;
    }

    public string Path => _hotspot.Path;
    public string DurationText => $"{_hotspot.DurationSeconds:N2}s total scan time across {_hotspot.ScanCount} scan(s)";
    public string? DevReason => _hotspot.DevReason;
    public bool IsDevRelated => _hotspot.IsDevRelated;
    public bool AlreadyExcluded => _hotspot.AlreadyExcluded;
    public bool CanExclude => IsDevRelated && !AlreadyExcluded;

    public Visibility DevBadgeVisibility => IsDevRelated ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExcludeButtonVisibility => CanExclude ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AlreadyExcludedVisibility => AlreadyExcluded ? Visibility.Visible : Visibility.Collapsed;

    public SolidColorBrush AccentBrush => new(IsDevRelated ? Colors.SeaGreen : Colors.SteelBlue);
}

/// <summary>UI-friendly wrapper around a process hotspot for x:Bind, with an "Exclude" action.</summary>
public sealed class DefenderProcessHotspotViewModel
{
    private readonly DefenderProcessHotspot _hotspot;

    public DefenderProcessHotspotViewModel(DefenderProcessHotspot hotspot)
    {
        _hotspot = hotspot;
    }

    public string ProcessPath => _hotspot.ProcessPath;
    public string DurationText => $"{_hotspot.DurationSeconds:N2}s total scan time across {_hotspot.ScanCount} scan(s)";
    public string? DevReason => _hotspot.DevReason;
    public bool IsDevRelated => _hotspot.IsDevRelated;
    public bool AlreadyExcluded => _hotspot.AlreadyExcluded;
    public bool CanExclude => IsDevRelated && !AlreadyExcluded;

    public Visibility DevBadgeVisibility => IsDevRelated ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ExcludeButtonVisibility => CanExclude ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AlreadyExcludedVisibility => AlreadyExcluded ? Visibility.Visible : Visibility.Collapsed;

    public SolidColorBrush AccentBrush => new(IsDevRelated ? Colors.SeaGreen : Colors.SteelBlue);
}
