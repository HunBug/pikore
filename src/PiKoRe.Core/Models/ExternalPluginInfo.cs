namespace PiKoRe.Core.Models;

/// <summary>
/// Metadata record for an externally-registered plugin service.
/// Stored in the plugin_registry SQLite table. Never used for dispatch —
/// dispatch always goes through IInProcessPlugin (see D-021, D-022).
/// </summary>
public sealed record ExternalPluginInfo(
    Guid Id,
    string Name,
    string Version,
    Uri Endpoint,
    IReadOnlyList<string> CapabilitiesProduced,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<string> SupportedMediaTypes,
    int GpuMemoryMb,
    DateTimeOffset RegisteredAt
);
