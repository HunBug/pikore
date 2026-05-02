namespace PiKoRe.Core.Abstractions;

/// <summary>Manages the set of known plugins and capability routing.</summary>
public interface IPluginRegistry
{
    Task RegisterAsync(IExternalPlugin plugin, CancellationToken ct);
    Task DeregisterAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<IPlugin>> GetAllAsync(CancellationToken ct);
    Task<IPlugin?> GetByCapabilityAsync(string capability, CancellationToken ct);
}
