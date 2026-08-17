using CheckDevHealth.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CheckDevHealth.Views;

/// <summary>Thin UI-friendly wrapper around <see cref="CheckResult"/> for x:Bind.</summary>
public sealed class CheckResultViewModel
{
    private readonly CheckResult _result;

    public CheckResultViewModel(CheckResult result)
    {
        _result = result;
    }

    public string Name => _result.Name;
    public string Value => _result.Value;
    public string Recommendation => _result.Recommendation ?? string.Empty;
    public Visibility HasRecommendation => string.IsNullOrEmpty(_result.Recommendation) ? Visibility.Collapsed : Visibility.Visible;
    public string Glyph => _result.StatusGlyph;

    public SolidColorBrush StatusBrush => new(_result.Status switch
    {
        CheckStatus.Ok => Colors.SeaGreen,
        CheckStatus.Info => Colors.SteelBlue,
        CheckStatus.Warning => Colors.DarkOrange,
        CheckStatus.Critical => Colors.Crimson,
        CheckStatus.Error => Colors.Crimson,
        _ => Colors.Gray
    });
}

/// <summary>Groups results by category for display.</summary>
public sealed class CategoryGroup
{
    public required string CategoryName { get; init; }
    public required IReadOnlyList<CheckResultViewModel> Items { get; init; }
}
