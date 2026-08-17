using System.Text.Json.Serialization;
using CheckDevHealth.Models;
using CheckDevHealth.Services.Analysis;
using CheckDevHealth.Services.DefenderHotspot;

namespace CheckDevHealth.Services;

/// <summary>
/// Source-generated JSON (de)serialization metadata for every type this app passes through
/// <see cref="System.Text.Json.JsonSerializer"/>. Required because the published/MSIX-packaged
/// build enables trimming (<c>PublishTrimmed</c>), which disables the reflection-based
/// serializer by default — without this context, calls throw
/// "reflection-based serialization has been disabled for this application".
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(MpOverviewRoot))]
[JsonSerializable(typeof(MpTopFilesRoot))]
[JsonSerializable(typeof(MpTopProcessesRoot))]
[JsonSerializable(typeof(CloudChatRequest))]
[JsonSerializable(typeof(CloudChatResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
