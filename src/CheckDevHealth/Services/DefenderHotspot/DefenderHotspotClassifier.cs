namespace CheckDevHealth.Services.DefenderHotspot;

/// <summary>
/// Heuristics for recognizing common developer folders/tools in Defender performance
/// report output, so hotspots can be flagged as "safe to consider for exclusion" candidates.
/// This is intentionally conservative: it never auto-excludes anything, it only labels.
/// </summary>
internal static class DefenderHotspotClassifier
{
    // Path substrings commonly associated with dev tool caches/build output. Matching is
    // case-insensitive and substring-based since these can appear at any depth under a
    // user's repo or profile folder.
    private static readonly (string Pattern, string Reason)[] DevPathPatterns =
    {
        (@"\node_modules\", "npm/Node.js package cache — rebuilt by 'npm install', safe to exclude for build speed."),
        (@"\node_modules", "npm/Node.js package cache — rebuilt by 'npm install', safe to exclude for build speed."),
        (@"\.git\", "Git repository metadata — frequently touched during checkouts/builds/CI."),
        (@"\bin\debug\", "Build output folder (Debug) — regenerated on every build."),
        (@"\bin\release\", "Build output folder (Release) — regenerated on every build."),
        (@"\obj\", "MSBuild intermediate/object folder — regenerated on every build."),
        (@"\.vs\", "Visual Studio hidden solution cache folder."),
        (@"\.nuget\packages\", "NuGet global package cache — rebuilt on demand by restore."),
        (@"\npm-cache\", "npm package cache."),
        (@"\pnpm\store\", "pnpm content-addressable package store."),
        (@"\yarn\cache\", "Yarn package cache."),
        (@"\pip\cache\", "pip package cache."),
        (@"\.cargo\", "Rust Cargo registry/build cache."),
        (@"\target\", "Rust/Maven build output folder — regenerated on every build."),
        (@"\.gradle\", "Gradle cache/daemon working folder."),
        (@"\go\pkg\", "Go module cache."),
        (@"\.m2\", "Maven local repository cache."),
        (@"\testresults\", "Test runner output/temp folder."),
        (@"\appdata\local\temp\", "User temp folder — heavy transient I/O from build/package tools."),
        (@"\dist\", "Bundler/compiler output folder (webpack/vite/tsc/etc)."),
        (@"\.next\", "Next.js build cache."),
        (@"\.venv\", "Python virtual environment folder."),
        (@"\venv\", "Python virtual environment folder."),
        (@"\docker\", "Docker Desktop data (VHDX/overlay storage)."),
        (@"\wsl\", "Windows Subsystem for Linux distro storage (vhdx)."),
    };

    // Executable names commonly responsible for heavy dev-related I/O; matched against the
    // file name portion of ProcessPath only (never the full path, to avoid false path matches).
    private static readonly (string ExeName, string Reason)[] DevProcessNames =
    {
        ("devenv.exe", "Visual Studio IDE — indexing/build/IntelliSense I/O."),
        ("msbuild.exe", "MSBuild — compiles and writes build output on every build."),
        ("dotnet.exe", ".NET CLI — build/restore/run operations."),
        ("vbcscompiler.exe", ".NET Roslyn build server (shared compiler process)."),
        ("node.exe", "Node.js — used by npm/yarn/pnpm/bundlers and dev servers."),
        ("npm.exe", "npm CLI — installs/writes many small package files."),
        ("npm.cmd", "npm CLI — installs/writes many small package files."),
        ("yarn.exe", "Yarn CLI — package installs."),
        ("pnpm.exe", "pnpm CLI — package installs."),
        ("git.exe", "Git — repository read/write operations."),
        ("python.exe", "Python interpreter — package installs/build scripts."),
        ("pip.exe", "pip — Python package installs."),
        ("java.exe", "Java runtime — used by Gradle/Maven/Android tooling."),
        ("javac.exe", "Java compiler."),
        ("gradle.exe", "Gradle build tool."),
        ("go.exe", "Go toolchain — build/test/module downloads."),
        ("docker.exe", "Docker CLI."),
        ("dockerd.exe", "Docker Engine daemon."),
        ("com.docker.backend.exe", "Docker Desktop backend service."),
        ("vmmem", "Virtual machine memory process used by WSL2/Docker Desktop."),
        ("rustc.exe", "Rust compiler."),
        ("cargo.exe", "Rust Cargo build tool."),
        ("code.exe", "Visual Studio Code — file watching/extension I/O."),
        ("rider64.exe", "JetBrains Rider IDE."),
        ("cl.exe", "MSVC C/C++ compiler."),
        ("link.exe", "MSVC linker."),
        ("ninja.exe", "Ninja build system."),
        ("cmake.exe", "CMake build generator."),
        ("webpack.exe", "Webpack bundler."),
        ("esbuild.exe", "esbuild bundler."),
        ("vite.exe", "Vite dev server/bundler."),
    };

    public static bool TryClassifyPath(string path, out string? reason)
    {
        var normalized = path.ToLowerInvariant();
        foreach (var (pattern, patternReason) in DevPathPatterns)
        {
            if (normalized.Contains(pattern, StringComparison.Ordinal))
            {
                reason = patternReason;
                return true;
            }
        }

        reason = null;
        return false;
    }

    public static bool TryClassifyProcess(string processPath, out string? reason)
    {
        var fileName = System.IO.Path.GetFileName(processPath);
        foreach (var (exeName, exeReason) in DevProcessNames)
        {
            if (string.Equals(fileName, exeName, StringComparison.OrdinalIgnoreCase))
            {
                reason = exeReason;
                return true;
            }
        }

        reason = null;
        return false;
    }
}
