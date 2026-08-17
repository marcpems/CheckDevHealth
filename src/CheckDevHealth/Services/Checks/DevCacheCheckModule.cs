using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>Sizes of common developer caches that tend to silently consume disk space over time.</summary>
public sealed class DevCacheCheckModule : ICheckModule
{
    public string ModuleName => "Developer Caches";

    public Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        // Walking large caches (e.g. a multi-GB NuGet/npm cache) synchronously can take a while;
        // run it on a thread-pool thread so the UI thread/message pump stays responsive.
        return Task.Run(() => RunCore(cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<CheckResult> RunCore(CancellationToken cancellationToken)
    {
        const string category = "Developer Caches";
        var results = new List<CheckResult>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var candidates = new (string Label, string Path)[]
        {
            ("NuGet packages cache", Path.Combine(userProfile, ".nuget", "packages")),
            ("npm cache", Path.Combine(localAppData, "npm-cache")),
            ("npm cache (roaming)", Path.Combine(roamingAppData, "npm-cache")),
            ("pip cache", Path.Combine(localAppData, "pip", "Cache")),
            ("User Temp folder", Path.Combine(localAppData, "Temp")),
            ("pnpm store", Path.Combine(localAppData, "pnpm", "store")),
            ("Yarn cache", Path.Combine(localAppData, "Yarn", "Cache")),
        };

        foreach (var (label, path) in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(path))
            {
                continue;
            }

            var sizeBytes = GetDirectorySizeSafe(path);
            var sizeGb = sizeBytes / 1024.0 / 1024.0 / 1024.0;
            if (sizeGb < 0.1)
            {
                continue; // not worth reporting
            }

            results.Add(new CheckResult
            {
                Category = category,
                Name = label,
                Status = sizeGb > 5 ? CheckStatus.Warning : CheckStatus.Info,
                Value = $"{sizeGb:N1} GB — {path}",
                Recommendation = sizeGb > 5 ? "This cache is large and safe to clear if disk space is tight; it will be rebuilt on demand." : null
            });
        }

        return results;
    }

    private static long GetDirectorySizeSafe(string path)
    {
        long total = 0;
        try
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, ReturnSpecialDirectories = false };
            foreach (var file in Directory.EnumerateFiles(path, "*", options))
            {
                try { total += new FileInfo(file).Length; } catch { /* skip locked/inaccessible files */ }
            }
        }
        catch
        {
            // ignore inaccessible root
        }

        return total;
    }
}
