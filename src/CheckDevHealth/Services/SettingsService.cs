using System.Text.Json;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> from the user's local app data folder,
/// so settings survive across app updates and are per-user.
/// </summary>
public sealed class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CheckDevHealth");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Fall back to defaults if the file is missing/corrupt.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, AppJsonContext.Default.AppSettings);
        File.WriteAllText(SettingsPath, json);
    }
}
