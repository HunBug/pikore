using PiKoRe.Core.Models;

namespace PiKoRe.Core.Abstractions;

/// <summary>
/// Stores metadata for externally-registered plugin services.
/// Used by the registration endpoint and by adapter plugins that need
/// to resolve a service endpoint at runtime. Not used for dispatch —
/// dispatch is always IInProcessPlugin (see D-021, D-022).
/// </summary>
public interface IPluginRegistry
{
    Task RegisterAsync(ExternalPluginInfo plugin, CancellationToken ct);
    Task DeregisterAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<ExternalPluginInfo>> GetAllAsync(CancellationToken ct);
    Task<ExternalPluginInfo?> GetByNameAsync(string name, CancellationToken ct);
}
