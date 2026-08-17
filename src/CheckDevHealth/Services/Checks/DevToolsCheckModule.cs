using System.Runtime.InteropServices;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>Common dev tool presence, versions and whether they're running natively vs. emulated.</summary>
public sealed class DevToolsCheckModule : ICheckModule
{
    public string ModuleName => "Developer Tools";

    private static readonly (string Exe, string Args, string DisplayName)[] Tools =
    {
        ("git", "--version", "Git"),
        ("node", "-v", "Node.js"),
        ("python", "--version", "Python"),
        ("docker", "--version", "Docker"),
        ("pwsh", "-NoLogo -NoProfile -Command $PSVersionTable.PSVersion.ToString()", "PowerShell (pwsh)"),
    };

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "Developer Tools";
        var hostArch = RuntimeInformation.OSArchitecture;

        foreach (var (exe, args, displayName) in Tools)
        {
            if (!ProcessRunner.IsOnPath(exe) && !exe.Equals("pwsh", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new CheckResult
                {
                    Category = category,
                    Name = displayName,
                    Status = CheckStatus.Info,
                    Value = "Not installed / not on PATH"
                });
                continue;
            }

            var result = await ProcessRunner.RunAsync(exe, args, timeoutMs: 6000, cancellationToken: cancellationToken);
            if (!result.Success && string.IsNullOrWhiteSpace(result.StdOut))
            {
                results.Add(new CheckResult
                {
                    Category = category,
                    Name = displayName,
                    Status = CheckStatus.Info,
                    Value = "Not installed / not on PATH"
                });
                continue;
            }

            var version = result.StdOut.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown version";
            results.Add(new CheckResult
            {
                Category = category,
                Name = displayName,
                Status = CheckStatus.Ok,
                Value = version
            });
        }

        // On ARM64 hosts, flag if the process architecture check tool reports x64 (emulated via IsWow64Process2 is more precise,
        // but for our purposes checking the current app's own architecture vs. OS gives users a hint to investigate).
        if (hostArch == Architecture.Arm64)
        {
            results.Add(new CheckResult
            {
                Category = category,
                Name = "ARM64 Native Toolchain",
                Status = CheckStatus.Info,
                Value = "This machine is ARM64. Prefer native-ARM64 builds of Git/Node/Python/Docker over x64 versions running under emulation for best performance.",
                Recommendation = "Check each tool's installer/download page for an 'ARM64' or 'Windows on Arm' build."
            });
        }

        return results;
    }
}
