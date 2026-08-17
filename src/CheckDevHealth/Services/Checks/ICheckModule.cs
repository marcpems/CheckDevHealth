using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>
/// Common contract for a single area of the machine sweep (CPU, disk, Defender, etc).
/// Implement this to add a new check category to the app.
/// </summary>
public interface ICheckModule
{
    /// <summary>Display name of the module, used for progress reporting.</summary>
    string ModuleName { get; }

    /// <summary>Runs the checks and returns zero or more results.</summary>
    Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken);
}
