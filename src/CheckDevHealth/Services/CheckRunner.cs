using CheckDevHealth.Models;
using CheckDevHealth.Services.Checks;

namespace CheckDevHealth.Services;

/// <summary>
/// Runs every registered <see cref="ICheckModule"/> and aggregates the results into one flat list.
/// Add new modules to the <see cref="Modules"/> list to extend the sweep.
/// </summary>
public sealed class CheckRunner
{
    private static readonly IReadOnlyList<ICheckModule> Modules = new List<ICheckModule>
    {
        new SystemInfoCheckModule(),
        new CpuMemoryCheckModule(),
        new DiskCheckModule(),
        new DevToolsCheckModule(),
        new PlatformFeaturesCheckModule(),
        new DefenderCheckModule(),
        new PowerAndMemoryConfigCheckModule(),
        new DevCacheCheckModule(),
    };

    public async Task<IReadOnlyList<CheckResult>> RunAllAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var all = new List<CheckResult>();

        foreach (var module in Modules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(module.ModuleName);

            try
            {
                var results = await module.RunAsync(cancellationToken);
                all.AddRange(results);
            }
            catch (Exception ex)
            {
                all.Add(new CheckResult
                {
                    Category = module.ModuleName,
                    Name = module.ModuleName,
                    Status = CheckStatus.Error,
                    Value = $"Check failed: {ex.Message}"
                });
            }
        }

        return all;
    }
}
