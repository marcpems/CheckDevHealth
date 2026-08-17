namespace CheckDevHealth.Models;

public enum CheckStatus
{
    Ok,
    Info,
    Warning,
    Critical,
    Error
}

/// <summary>
/// Result of a single check performed against the machine.
/// </summary>
public sealed class CheckResult
{
    public required string Category { get; init; }
    public required string Name { get; init; }
    public required CheckStatus Status { get; init; }
    public required string Value { get; init; }
    public string? Recommendation { get; init; }

    public string StatusGlyph => Status switch
    {
        CheckStatus.Ok => "\uE73E",       // CheckMark
        CheckStatus.Info => "\uE946",     // Info
        CheckStatus.Warning => "\uE7BA",  // Warning
        CheckStatus.Critical => "\uEA39", // Error/Critical
        CheckStatus.Error => "\uE783",    // ReportHacked-ish (used for check failures)
        _ => "\uE946"
    };
}
