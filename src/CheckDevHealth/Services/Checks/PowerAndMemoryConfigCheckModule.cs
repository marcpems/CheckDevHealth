using System.Text.Json;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>Power plan and page file configuration.</summary>
public sealed class PowerAndMemoryConfigCheckModule : ICheckModule
{
    public string ModuleName => "Power & Paging";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "Power & Paging";

        var powerResult = await ProcessRunner.RunAsync("powercfg.exe", "/getactivescheme", timeoutMs: 5000, cancellationToken: cancellationToken);
        var planLine = powerResult.StdOut;
        var isBalanced = planLine.Contains("Balanced", StringComparison.OrdinalIgnoreCase);
        var isPowerSaver = planLine.Contains("Power saver", StringComparison.OrdinalIgnoreCase);

        results.Add(new CheckResult
        {
            Category = category,
            Name = "Active Power Plan",
            Status = isPowerSaver ? CheckStatus.Warning : CheckStatus.Info,
            Value = string.IsNullOrWhiteSpace(planLine) ? "Unknown" : planLine.Split('(', ')').ElementAtOrDefault(1) ?? planLine,
            Recommendation = isPowerSaver
                ? "Power Saver throttles CPU aggressively; switch to Balanced or Best/High Performance while doing builds/compiles on AC power."
                : (isBalanced ? "Balanced is fine for most work; consider 'Best Performance' while plugged in for sustained heavy builds." : null)
        });

        var pageFileJson = await PowerShellJson.RunAsync(
            "Get-CimInstance Win32_PageFileUsage | Select-Object Name,AllocatedBaseSize | ConvertTo-Json -Compress",
            cancellationToken);

        if (pageFileJson is { ValueKind: JsonValueKind.Object } pf)
        {
            var name = pf.TryGetProperty("Name", out var n) ? n.GetString() : "Unknown";
            var sizeMb = pf.TryGetProperty("AllocatedBaseSize", out var s) ? s.GetInt64() : 0;
            results.Add(new CheckResult
            {
                Category = category,
                Name = "Page File",
                Status = CheckStatus.Info,
                Value = $"{name}: {sizeMb / 1024.0:N1} GB allocated"
            });
        }

        return results;
    }
}
