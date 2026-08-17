using System.Text.Json;

namespace CheckDevHealth.Services.Checks;

/// <summary>
/// Runs a PowerShell script block and parses its "ConvertTo-Json" output.
/// Using PowerShell/CIM under the hood keeps each check module small and lets us
/// reuse the same well-tested cmdlets (Get-CimInstance, Get-Volume, Get-MpPreference, ...)
/// instead of hand-rolling WMI/P-Invoke for every data point.
/// </summary>
internal static class PowerShellJson
{
    public static async Task<JsonElement?> RunAsync(string script, CancellationToken cancellationToken = default)
    {
        // Wrap in ConvertTo-Json -Depth so nested objects/arrays survive; -Compress keeps it small.
        var wrapped = $"$ProgressPreference='SilentlyContinue'; try {{ {script} }} catch {{ }}";
        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{wrapped.Replace("\"", "\\\"")}\"";

        var result = await ProcessRunner.RunAsync("powershell.exe", args, timeoutMs: 15000, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(result.StdOut))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(result.StdOut);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}
