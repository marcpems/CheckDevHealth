using System.Diagnostics;
using System.Text;

namespace CheckDevHealth.Services.Checks;

/// <summary>
/// Small helper for running console tools (git, node, wsl, fsutil, etc.) and capturing output.
/// Shared by several check modules.
/// </summary>
internal static class ProcessRunner
{
    public sealed record Result(bool Success, string StdOut, string StdErr, int ExitCode);

    public static async Task<Result> RunAsync(string fileName, string arguments, int timeoutMs = 8000, CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdOut.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stdErr.AppendLine(e.Data); };

            if (!process.Start())
            {
                return new Result(false, string.Empty, "Failed to start process.", -1);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                return new Result(false, stdOut.ToString(), "Timed out.", -1);
            }

            return new Result(process.ExitCode == 0, stdOut.ToString().Trim(), stdErr.ToString().Trim(), process.ExitCode);
        }
        catch (Exception ex)
        {
            return new Result(false, string.Empty, ex.Message, -1);
        }
    }

    /// <summary>Locates an executable on PATH without throwing if it's missing.</summary>
    public static bool IsOnPath(string exeName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathExt = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".exe").Split(';');

        foreach (var dir in pathVar.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in pathExt)
            {
                var candidate = Path.Combine(dir.Trim(), exeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? exeName : exeName + ext);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
