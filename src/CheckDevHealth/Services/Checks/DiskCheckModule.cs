using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>Physical disk media type/health, volume free space, and Dev Drive detection.</summary>
public sealed class DiskCheckModule : ICheckModule
{
    public string ModuleName => "Disks & Volumes";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "Storage";

        var disksJson = await PowerShellJson.RunAsync(
            "Get-PhysicalDisk | Select-Object FriendlyName,MediaType,HealthStatus,@{n='SizeGB';e={[math]::Round($_.Size/1GB,0)}} | ConvertTo-Json -Compress",
            cancellationToken);

        foreach (var disk in EnumerateArrayOrSingle(disksJson))
        {
            var friendly = disk.TryGetProperty("FriendlyName", out var fn) ? fn.GetString() : "Disk";
            var media = disk.TryGetProperty("MediaType", out var mt) ? mt.GetString() : "Unknown";
            var health = disk.TryGetProperty("HealthStatus", out var hs) ? hs.GetString() : "Unknown";
            var sizeGb = disk.TryGetProperty("SizeGB", out var sz) ? sz.GetDouble() : 0;

            var isHdd = string.Equals(media, "HDD", StringComparison.OrdinalIgnoreCase);
            var unhealthy = !string.Equals(health, "Healthy", StringComparison.OrdinalIgnoreCase);

            results.Add(new CheckResult
            {
                Category = category,
                Name = $"Physical Disk: {friendly}",
                Status = unhealthy ? CheckStatus.Critical : (isHdd ? CheckStatus.Warning : CheckStatus.Ok),
                Value = $"{media}, {sizeGb:N0} GB, Health: {health}",
                Recommendation = unhealthy
                    ? "Disk health is not reporting Healthy — back up data and run vendor diagnostics."
                    : (isHdd ? "Spinning HDD detected. Builds, git operations and IDE indexing are much faster on an SSD/NVMe." : null)
            });
        }

        var volumesJson = await PowerShellJson.RunAsync(
            "Get-Volume | Where-Object { $_.DriveLetter } | Select-Object DriveLetter,FileSystemType,@{n='SizeGB';e={[math]::Round($_.Size/1GB,1)}},@{n='FreeGB';e={[math]::Round($_.SizeRemaining/1GB,1)}} | ConvertTo-Json -Compress",
            cancellationToken);

        foreach (var vol in EnumerateArrayOrSingle(volumesJson))
        {
            var letter = vol.TryGetProperty("DriveLetter", out var dl) ? dl.GetString() : "?";
            var fsType = vol.TryGetProperty("FileSystemType", out var fs) ? fs.GetString() : "";
            var sizeGb = vol.TryGetProperty("SizeGB", out var sz) ? sz.GetDouble() : 0;
            var freeGb = vol.TryGetProperty("FreeGB", out var fg) ? fg.GetDouble() : 0;
            var freePct = sizeGb > 0 ? freeGb / sizeGb * 100 : 100;

            var isDevDrive = await IsDevDriveAsync($"{letter}:\\", cancellationToken);

            var status = freePct < 10 ? CheckStatus.Critical : (freePct < 20 ? CheckStatus.Warning : CheckStatus.Ok);

            results.Add(new CheckResult
            {
                Category = category,
                Name = $"Volume {letter}:\\ ({fsType})" + (isDevDrive ? " — Dev Drive" : ""),
                Status = status,
                Value = $"{freeGb:N1} GB free of {sizeGb:N1} GB ({freePct:N0}% free)",
                Recommendation = status != CheckStatus.Ok
                    ? $"Free space on {letter}:\\ is low ({freePct:N0}%). Low disk space slows the OS (paging, updates, fragmentation) and can block builds/package installs."
                    : (isDevDrive ? "Already configured as a Dev Drive (ReFS + Defender performance mode) — good for source/build folders." : null)
            });
        }

        return results;
    }

    private static async Task<bool> IsDevDriveAsync(string root, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("fsutil.exe", $"devdrv query {root}", timeoutMs: 5000, cancellationToken: cancellationToken);
        return result.StdOut.Contains("trusted developer volume", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<System.Text.Json.JsonElement> EnumerateArrayOrSingle(System.Text.Json.JsonElement? element)
    {
        if (element is not { } el)
        {
            yield break;
        }

        if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                yield return item;
            }
        }
        else if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            yield return el;
        }
    }
}
