using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using CheckDevHealth.Models;
using CheckDevHealth.Services.Checks;

namespace CheckDevHealth.Services.DefenderHotspot;

/// <summary>
/// Records a Microsoft Defender Antivirus performance trace (via New-MpPerformanceRecording)
/// while the caller runs a build/heavy workload, then analyzes the trace (via
/// Get-MpPerformanceReport) to find which folders/processes cost the most real-time scan time,
/// and flags the ones that look like common developer tool caches/build output.
///
/// This never changes Defender configuration itself — it only reports candidates. Adding an
/// exclusion is a separate, explicit, user-confirmed action (see <see cref="AddExclusionPathAsync"/>).
/// </summary>
public sealed class DefenderHotspotService
{
    /// <summary>True if the current process is running elevated. Recording requires admin rights.</summary>
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async Task<DefenderHotspotReport> RecordAndAnalyzeAsync(
        int seconds,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (!IsElevated())
        {
            throw new InvalidOperationException(
                "Recording a Defender performance trace requires administrator privileges. Restart Check Dev Health as an administrator and try again.");
        }

        var etlPath = Path.Combine(Path.GetTempPath(), $"CheckDevHealth-{Guid.NewGuid():N}.etl");

        try
        {
            progress?.Report($"Recording Defender activity for {seconds}s — run your build/workload now...");

            var recordArgs =
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                $"\"New-MpPerformanceRecording -RecordTo '{etlPath}' -Seconds {seconds} | Out-Null\"";

            // Recording blocks for the requested duration plus a little overhead for wpr start/stop.
            var recordResult = await ProcessRunner.RunAsync(
                "powershell.exe", recordArgs, timeoutMs: (seconds + 30) * 1000, cancellationToken: cancellationToken);

            if (!recordResult.Success || !File.Exists(etlPath))
            {
                throw new InvalidOperationException(
                    $"Failed to record Defender performance trace: {(string.IsNullOrWhiteSpace(recordResult.StdErr) ? "unknown error" : recordResult.StdErr)}");
            }

            progress?.Report("Analyzing recording...");

            var (overview, topFiles, topProcesses) = await AnalyzeTraceAsync(etlPath, cancellationToken);
            var (exclusionPaths, exclusionProcesses) = await GetCurrentExclusionsAsync(cancellationToken);

            var pathHotspots = BuildPathHotspots(topFiles, exclusionPaths);
            var processHotspots = BuildProcessHotspots(topProcesses, exclusionProcesses);

            return new DefenderHotspotReport
            {
                RecordingSeconds = seconds,
                RealTimeScanCount = overview?.RealTimeScans ?? 0,
                TotalRealTimeScanDurationMs = overview is null ? 0 : MpJson.TicksToMs(overview.RealTimeScansDuration),
                Paths = pathHotspots,
                Processes = processHotspots,
                CurrentExclusionPaths = exclusionPaths,
                CurrentExclusionProcesses = exclusionProcesses
            };
        }
        finally
        {
            try { if (File.Exists(etlPath)) File.Delete(etlPath); } catch { /* best effort cleanup */ }
        }
    }

    private static async Task<(MpOverview? overview, List<MpFileEntry> files, List<MpProcessEntry> processes)> AnalyzeTraceAsync(
        string etlPath, CancellationToken cancellationToken)
    {
        var overviewJson = await RunReportAsync(etlPath, "-Overview", cancellationToken);
        var overview = overviewJson is null
            ? null
            : JsonSerializer.Deserialize<MpOverviewRoot>(overviewJson, MpJson.Options)?.Overview;

        var filesJson = await RunReportAsync(etlPath, "-TopFiles 300", cancellationToken);
        var files = filesJson is null
            ? new List<MpFileEntry>()
            : JsonSerializer.Deserialize<MpTopFilesRoot>(filesJson, MpJson.Options)?.TopFiles ?? new List<MpFileEntry>();

        var processesJson = await RunReportAsync(etlPath, "-TopProcesses 50", cancellationToken);
        var processes = processesJson is null
            ? new List<MpProcessEntry>()
            : JsonSerializer.Deserialize<MpTopProcessesRoot>(processesJson, MpJson.Options)?.TopProcesses ?? new List<MpProcessEntry>();

        return (overview, files, processes);
    }

    private static async Task<string?> RunReportAsync(string etlPath, string reportArgs, CancellationToken cancellationToken)
    {
        var args =
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
            $"\"Get-MpPerformanceReport -Path '{etlPath}' {reportArgs} -Raw | ConvertTo-Json -Depth 4 -Compress\"";

        var result = await ProcessRunner.RunAsync("powershell.exe", args, timeoutMs: 30000, cancellationToken: cancellationToken);
        return string.IsNullOrWhiteSpace(result.StdOut) ? null : result.StdOut;
    }

    private static async Task<(List<string> paths, List<string> processes)> GetCurrentExclusionsAsync(CancellationToken cancellationToken)
    {
        var pathsJson = await PowerShellJson.RunAsync(
            "Get-MpPreference | Select-Object -ExpandProperty ExclusionPath | ConvertTo-Json -Compress", cancellationToken);
        var processesJson = await PowerShellJson.RunAsync(
            "Get-MpPreference | Select-Object -ExpandProperty ExclusionProcess | ConvertTo-Json -Compress", cancellationToken);

        return (ToStringList(pathsJson), ToStringList(processesJson));
    }

    private static List<string> ToStringList(JsonElement? element) => element switch
    {
        { ValueKind: JsonValueKind.Array } arr => arr.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList(),
        { ValueKind: JsonValueKind.String } str => new List<string> { str.GetString() ?? string.Empty },
        _ => new List<string>()
    };

    /// <summary>
    /// Groups the flat top-files list by containing folder (one level up from the file) so the
    /// UI shows actionable "exclude this folder" candidates instead of hundreds of individual files.
    /// </summary>
    private static IReadOnlyList<DefenderPathHotspot> BuildPathHotspots(List<MpFileEntry> files, List<string> currentExclusions)
    {
        var groups = files
            .Where(f => !string.IsNullOrWhiteSpace(f.Path))
            .GroupBy(f => Path.GetDirectoryName(f.Path) ?? f.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Folder = g.Key,
                DurationMs = g.Sum(f => MpJson.TicksToMs(f.TotalDuration)),
                ScanCount = g.Sum(f => f.Count)
            })
            .OrderByDescending(g => g.DurationMs)
            .Take(25)
            .ToList();

        var results = new List<DefenderPathHotspot>();
        foreach (var g in groups)
        {
            var isDev = DefenderHotspotClassifier.TryClassifyPath(g.Folder, out var reason);
            var alreadyExcluded = currentExclusions.Any(e =>
                !string.IsNullOrWhiteSpace(e) && g.Folder.StartsWith(e.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));

            results.Add(new DefenderPathHotspot
            {
                Path = g.Folder,
                DurationMs = g.DurationMs,
                ScanCount = g.ScanCount,
                IsDevRelated = isDev,
                DevReason = reason,
                AlreadyExcluded = alreadyExcluded
            });
        }

        return results;
    }

    private static IReadOnlyList<DefenderProcessHotspot> BuildProcessHotspots(List<MpProcessEntry> processes, List<string> currentExclusions)
    {
        var results = new List<DefenderProcessHotspot>();
        foreach (var p in processes.Where(p => !string.IsNullOrWhiteSpace(p.ProcessPath)))
        {
            var isDev = DefenderHotspotClassifier.TryClassifyProcess(p.ProcessPath, out var reason);
            var fileName = Path.GetFileName(p.ProcessPath);
            var alreadyExcluded = currentExclusions.Any(e => string.Equals(e, fileName, StringComparison.OrdinalIgnoreCase)
                                                               || string.Equals(e, p.ProcessPath, StringComparison.OrdinalIgnoreCase));

            results.Add(new DefenderProcessHotspot
            {
                ProcessPath = p.ProcessPath,
                DurationMs = MpJson.TicksToMs(p.TotalDuration),
                ScanCount = p.Count,
                IsDevRelated = isDev,
                DevReason = reason,
                AlreadyExcluded = alreadyExcluded
            });
        }

        return results.OrderByDescending(r => r.DurationMs).ToList();
    }

    /// <summary>
    /// Adds a folder path exclusion via an elevated Add-MpPreference call. Triggers a UAC prompt
    /// since this changes system security configuration; the calling page should confirm with the
    /// user before invoking this.
    /// </summary>
    public static Task<bool> AddExclusionPathAsync(string path) =>
        RunElevatedPreferenceChangeAsync($"Add-MpPreference -ExclusionPath '{EscapeSingleQuotes(path)}'");

    /// <summary>Adds a process-name exclusion via an elevated Add-MpPreference call.</summary>
    public static Task<bool> AddExclusionProcessAsync(string processPath) =>
        RunElevatedPreferenceChangeAsync($"Add-MpPreference -ExclusionProcess '{EscapeSingleQuotes(processPath)}'");

    private static string EscapeSingleQuotes(string value) => value.Replace("'", "''");

    private static async Task<bool> RunElevatedPreferenceChangeAsync(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined the UAC prompt.
            return false;
        }
    }
}
