# Check Dev Health

A WinUI 3 desktop app that sweeps a Windows machine for developer-experience and
performance-related configuration issues (disk space, Dev Drive usage, Defender
exclusions, power plan, dev tool versions/architecture, WSL/Developer Mode, cache
bloat, etc.) and can optionally summarize the results using AI.

Runs natively on both **x64** and **ARM64** Windows machines.

## Solution layout

```
CheckDevHealth.slnx                     Solution file (x86 / x64 / ARM64)
src/CheckDevHealth/
  CheckDevHealth.csproj                 WinUI 3 (Windows App SDK) app project
  App.xaml(.cs), MainWindow.xaml(.cs)   App shell + NavigationView host
  Views/                                SweepPage, DefenderHotspotPage, AnalysisPage, SettingsPage
  Models/                               CheckResult, AppSettings, DefenderHotspotReport
  Services/
    CheckRunner.cs                      Orchestrates all check modules
    SettingsService.cs, AppState.cs     Settings persistence + shared state
    Checks/                             One class per check area (add new ones here)
    Analysis/                           Pluggable AI analysis (Cloud / Local)
    DefenderHotspot/                    Defender performance recording/report + classifier
```

## Adding a new check

Implement `Services.Checks.ICheckModule` and register it in
`Services/CheckRunner.cs`'s `Modules` list. Each module returns one or more
`CheckResult` items (category, name, status, value, optional recommendation).

## AI analysis: Cloud vs. Local

The "AI Analysis" page sends the sweep results to whichever `IAnalysisProvider`
is active, chosen in `Services/Analysis/AnalysisProviderFactory.cs` based on
`AppSettings.AnalysisMode`:

- **Cloud** (`CloudAnalysisProvider`): calls a configurable, OpenAI-compatible
  Chat Completions endpoint (OpenAI, Azure OpenAI, or a self-hosted proxy).
  Endpoint/model/API key are set on the Settings page and stored in
  `%LOCALAPPDATA%\CheckDevHealth\settings.json`.
- **Local** (`LocalAnalysisProvider`): placeholder for fully on-device/offline
  inference. Intentionally not implemented yet (see comments in that file for
  suggested approaches — Windows AI Foundry/Phi Silica, ONNX Runtime GenAI, or
  a localhost-only Ollama/llama.cpp server). It makes no network calls today.

Switch modes any time from the Settings page — no other code changes needed
to move between them once Local is implemented.

## Defender Hotspot Analysis

The "Defender Hotspots" page helps validate whether Microsoft Defender's
real-time protection is well-tuned for development work:

1. Choose a recording duration and click **Start Recording** while you run a
   build, `npm install`, `dotnet restore`, or other heavy workload.
2. The app calls `New-MpPerformanceRecording -RecordTo <temp.etl> -Seconds N`
   to capture a Defender scan trace, then `Get-MpPerformanceReport -Raw` to
   analyze which folders and processes consumed the most real-time scan time.
3. Hotspots are matched against a built-in list of common dev tool
   caches/build folders (`node_modules`, `bin`/`obj`, `.git`, NuGet/npm/pip/
   Cargo/Gradle caches, Docker/WSL storage, etc.) and dev executables
   (`msbuild.exe`, `node.exe`, `git.exe`, IDEs, compilers, ...).
4. Matches not already covered by an existing exclusion are offered an
   **Exclude Folder** / **Exclude Process** button, which — after an explicit
   confirmation — runs an elevated (UAC-prompted) `Add-MpPreference
   -ExclusionPath/-ExclusionProcess` call. Nothing is excluded automatically.

**Requires administrator privileges** to record a trace (`New-MpPerformanceRecording`
is an elevated-only cmdlet); the page shows a warning banner if the app isn't
running elevated. The temporary `.etl` trace file is deleted after analysis.

## Building

Requires Visual Studio 2022/2026 with the **.NET Desktop Development** and
**Windows App SDK / WinUI** workloads installed (the project relies on the
Windows App SDK's XAML compiler integration, which is not reliable when
building purely from the `dotnet` CLI on an ARM64 host).

```powershell
$msbuild = "<path to>\MSBuild.exe"   # e.g. under Visual Studio's MSBuild\Current\Bin\arm64
& $msbuild CheckDevHealth.slnx /p:Configuration=Debug /p:Platform=ARM64   # or x64 / x86
```

Or simply open `CheckDevHealth.slnx` in Visual Studio and press F5/Build.

Two run modes are available (see `Properties/launchSettings.json`):
- **CheckDevHealth (Unpackaged)** — runs as a plain .exe (`WindowsAppSDKSelfContained`
  is enabled, so the Windows App Runtime is bundled and no separate install is
  required on the target machine).
- **CheckDevHealth (Package)** — runs as an installed MSIX package using
  `Package.appxmanifest`.

## Publishing a portable build

Publish profiles for all three platforms are in `Properties/PublishProfiles/`.
Example:

```powershell
& $msbuild CheckDevHealth.slnx /t:Publish /p:Configuration=Release /p:Platform=ARM64
```

The output under `bin\ARM64\Release\net8.0-windows10.0.19041.0\win-arm64\publish\`
is self-contained (bundles both .NET and the Windows App Runtime) and can be
copied to another ARM64 machine and run directly. Use `Platform=x64` for x64
machines.
