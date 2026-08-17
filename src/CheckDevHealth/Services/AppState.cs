using CheckDevHealth.Models;

namespace CheckDevHealth.Services;

/// <summary>Simple in-memory app-wide state shared between pages (last sweep results, settings cache).</summary>
public static class AppState
{
    private static readonly SettingsService SettingsService = new();

    public static AppSettings Settings { get; private set; } = SettingsService.Load();

    public static IReadOnlyList<CheckResult> LastResults { get; set; } = Array.Empty<CheckResult>();

    public static void SaveSettings(AppSettings settings)
    {
        Settings = settings;
        SettingsService.Save(settings);
    }
}
