using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>CPU details and physical memory pressure.</summary>
public sealed class CpuMemoryCheckModule : ICheckModule
{
    public string ModuleName => "CPU & Memory";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "CPU & Memory";

        var cpuJson = await PowerShellJson.RunAsync(
            "Get-CimInstance Win32_Processor | Select-Object -First 1 Name,NumberOfCores,NumberOfLogicalProcessors | ConvertTo-Json -Compress",
            cancellationToken);

        if (cpuJson is { } cpu)
        {
            var name = cpu.TryGetProperty("Name", out var n) ? n.GetString() : "Unknown CPU";
            var cores = cpu.TryGetProperty("NumberOfCores", out var c) ? c.GetInt32() : 0;
            var logical = cpu.TryGetProperty("NumberOfLogicalProcessors", out var l) ? l.GetInt32() : 0;

            results.Add(new CheckResult
            {
                Category = category,
                Name = "Processor",
                Status = CheckStatus.Info,
                Value = $"{name?.Trim()} — {cores} cores / {logical} logical processors"
            });
        }

        var memJson = await PowerShellJson.RunAsync(
            "Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize,FreePhysicalMemory | ConvertTo-Json -Compress",
            cancellationToken);

        if (memJson is { } mem)
        {
            var totalKb = mem.TryGetProperty("TotalVisibleMemorySize", out var t) ? t.GetInt64() : 0;
            var freeKb = mem.TryGetProperty("FreePhysicalMemory", out var f) ? f.GetInt64() : 0;
            var totalGb = totalKb / 1024.0 / 1024.0;
            var freeGb = freeKb / 1024.0 / 1024.0;
            var freePct = totalKb > 0 ? (double)freeKb / totalKb * 100 : 0;

            var status = totalGb < 8 ? CheckStatus.Warning : (freePct < 10 ? CheckStatus.Warning : CheckStatus.Ok);

            results.Add(new CheckResult
            {
                Category = category,
                Name = "Physical Memory",
                Status = status,
                Value = $"{totalGb:N1} GB total, {freeGb:N1} GB free ({freePct:N0}%)",
                Recommendation = totalGb < 8
                    ? "Less than 8GB RAM detected; IDEs, containers and browsers together can cause heavy swapping during development."
                    : (freePct < 10 ? "Available memory is low right now; close unused apps/containers before large builds." : null)
            });
        }

        return results;
    }
}
