using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>WSL availability and Windows Developer Mode.</summary>
public sealed class PlatformFeaturesCheckModule : ICheckModule
{
    public string ModuleName => "Platform Features";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "Platform Features";

        var wsl = await ProcessRunner.RunAsync("wsl.exe", "--status", timeoutMs: 8000, cancellationToken: cancellationToken);
        var wslInstalled = wsl.Success && !string.IsNullOrWhiteSpace(wsl.StdOut);
        results.Add(new CheckResult
        {
            Category = category,
            Name = "WSL (Windows Subsystem for Linux)",
            Status = wslInstalled ? CheckStatus.Ok : CheckStatus.Info,
            Value = wslInstalled ? "Installed" : "Not installed",
            Recommendation = wslInstalled ? null : "Consider installing WSL2 ('wsl --install') for Linux-native tooling, containers, and faster POSIX file I/O."
        });

        var devModeJson = await PowerShellJson.RunAsync(
            "(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock' -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense | ConvertTo-Json -Compress",
            cancellationToken);

        var devModeOn = devModeJson is { } el && el.ValueKind is System.Text.Json.JsonValueKind.Number && el.GetInt32() == 1;
        results.Add(new CheckResult
        {
            Category = category,
            Name = "Developer Mode",
            Status = devModeOn ? CheckStatus.Ok : CheckStatus.Warning,
            Value = devModeOn ? "Enabled" : "Disabled",
            Recommendation = devModeOn ? null : "Enable Developer Mode in Settings > Privacy & Security > For Developers to allow symlinks without elevation, sideloading, and easier debugging."
        });

        return results;
    }
}
