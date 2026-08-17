using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckDevHealth.Services.DefenderHotspot;

// DTOs mirroring the JSON shape produced by `Get-MpPerformanceReport -Raw | ConvertTo-Json`.
// Field names match the raw PowerShell object properties (PascalCase), duration values are
// in 100-nanosecond ticks (same units as .NET TimeSpan.Ticks) per the ETW/WPR trace format.

internal sealed class MpOverviewRoot
{
    [JsonPropertyName("Overview")]
    public MpOverview? Overview { get; set; }
}

internal sealed class MpOverview
{
    public int RealTimeScans { get; set; }
    public long RealTimeScansDuration { get; set; }
}

internal sealed class MpTopFilesRoot
{
    [JsonPropertyName("TopFiles")]
    public List<MpFileEntry>? TopFiles { get; set; }
}

internal sealed class MpFileEntry
{
    public int Count { get; set; }
    public long TotalDuration { get; set; }
    public string Path { get; set; } = string.Empty;
}

internal sealed class MpTopProcessesRoot
{
    [JsonPropertyName("TopProcesses")]
    public List<MpProcessEntry>? TopProcesses { get; set; }
}

internal sealed class MpProcessEntry
{
    public int Count { get; set; }
    public long TotalDuration { get; set; }
    public string ProcessPath { get; set; } = string.Empty;
}

internal static class MpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Converts ETW duration ticks (100ns units) to milliseconds.</summary>
    public static double TicksToMs(long ticks) => ticks / 10000.0;
}
