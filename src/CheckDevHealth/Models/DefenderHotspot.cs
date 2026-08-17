namespace CheckDevHealth.Models;

/// <summary>Aggregated result of a single Defender performance recording + report pass.</summary>
public sealed class DefenderHotspotReport
{
    public int RecordingSeconds { get; init; }
    public double TotalRealTimeScanDurationMs { get; init; }
    public int RealTimeScanCount { get; init; }
    public IReadOnlyList<DefenderPathHotspot> Paths { get; init; } = Array.Empty<DefenderPathHotspot>();
    public IReadOnlyList<DefenderProcessHotspot> Processes { get; init; } = Array.Empty<DefenderProcessHotspot>();
    public IReadOnlyList<string> CurrentExclusionPaths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> CurrentExclusionProcesses { get; init; } = Array.Empty<string>();
}

/// <summary>A folder path that consumed a notable amount of Defender scan time.</summary>
public sealed class DefenderPathHotspot
{
    public required string Path { get; init; }
    public required double DurationMs { get; init; }
    public required int ScanCount { get; init; }
    public bool IsDevRelated { get; init; }
    public string? DevReason { get; init; }
    public bool AlreadyExcluded { get; init; }

    public double DurationSeconds => DurationMs / 1000.0;
}

/// <summary>A process image that generated a notable amount of Defender scan time.</summary>
public sealed class DefenderProcessHotspot
{
    public required string ProcessPath { get; init; }
    public required double DurationMs { get; init; }
    public required int ScanCount { get; init; }
    public bool IsDevRelated { get; init; }
    public string? DevReason { get; init; }
    public bool AlreadyExcluded { get; init; }

    public double DurationSeconds => DurationMs / 1000.0;
}
