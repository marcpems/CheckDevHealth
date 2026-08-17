using System.Runtime.InteropServices;
using CheckDevHealth.Models;

namespace CheckDevHealth.Services.Checks;

/// <summary>OS version/build, machine architecture and system type.</summary>
public sealed class SystemInfoCheckModule : ICheckModule
{
    public string ModuleName => "System Info";

    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken)
    {
        var results = new List<CheckResult>();
        const string category = "System";

        var json = await PowerShellJson.RunAsync(
            "Get-CimInstance Win32_OperatingSystem | Select-Object Caption,Version,BuildNumber,OSArchitecture | ConvertTo-Json -Compress",
            cancellationToken);

        if (json is { } el)
        {
            var caption = el.TryGetProperty("Caption", out var c) ? c.GetString() : "Unknown";
            var version = el.TryGetProperty("Version", out var v) ? v.GetString() : "";
            var build = el.TryGetProperty("BuildNumber", out var b) ? b.GetString() : "";
            var osArch = el.TryGetProperty("OSArchitecture", out var a) ? a.GetString() : "";

            var isPreview = (caption ?? string.Empty).Contains("Insider", StringComparison.OrdinalIgnoreCase);

            results.Add(new CheckResult
            {
                Category = category,
                Name = "Operating System",
                Status = isPreview ? CheckStatus.Warning : CheckStatus.Info,
                Value = $"{caption} (Build {build}, {osArch})".Trim(),
                Recommendation = isPreview
                    ? "Running an Insider Preview build. Expect occasional instability; avoid on production dev machines if stability is critical."
                    : null
            });
        }
        else
        {
            results.Add(new CheckResult { Category = category, Name = "Operating System", Status = CheckStatus.Error, Value = "Unable to query OS info." });
        }

        var processArch = RuntimeInformation.ProcessArchitecture;
        var osArchInfo = RuntimeInformation.OSArchitecture;
        results.Add(new CheckResult
        {
            Category = category,
            Name = "Machine Architecture",
            Status = CheckStatus.Info,
            Value = $"OS: {osArchInfo}, Check Dev Health process: {processArch}"
        });

        return results;
    }
}
