using System.Text.Json;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>Windows Defender real-time protection status and exclusion coverage.</summary>
public sealed class DefenderCheckModule : ICheckModule
{
    public string ModuleName => "Windows Defender";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "Security & Defender";

        var statusJson = await PowerShellJson.RunAsync(
            "Get-MpComputerStatus | Select-Object RealTimeProtectionEnabled,AntivirusEnabled | ConvertTo-Json -Compress",
            cancellationToken);

        if (statusJson is { } el)
        {
            var rtp = el.TryGetProperty("RealTimeProtectionEnabled", out var r) && r.ValueKind == JsonValueKind.True;
            results.Add(new CheckResult
            {
                Category = category,
                Name = "Real-time Protection",
                Status = CheckStatus.Info,
                Value = rtp ? "Enabled" : "Disabled"
            });
        }
        else
        {
            results.Add(new CheckResult { Category = category, Name = "Real-time Protection", Status = CheckStatus.Info, Value = "Unable to query (may require another AV product or admin rights)." });
        }

        var exclusionsJson = await PowerShellJson.RunAsync(
            "Get-MpPreference | Select-Object -ExpandProperty ExclusionPath | ConvertTo-Json -Compress",
            cancellationToken);

        var exclusionCount = exclusionsJson switch
        {
            { ValueKind: JsonValueKind.Array } arr => arr.GetArrayLength(),
            { ValueKind: JsonValueKind.String } => 1,
            _ => 0
        };

        results.Add(new CheckResult
        {
            Category = category,
            Name = "Defender Exclusion Paths",
            Status = exclusionCount == 0 ? CheckStatus.Info : CheckStatus.Ok,
            Value = exclusionCount == 0 ? "None configured" : $"{exclusionCount} path(s) excluded",
            Recommendation = exclusionCount == 0
                ? "Consider excluding source/build/node_modules folders (or use a Dev Drive, which gets Defender performance mode automatically) to speed up builds and package installs."
                : null
        });

        return results;
    }
}
