namespace PiKoRe.Core.Abstractions;

/// <summary>Represents a registered external plugin reachable via HTTP.</summary>
public interface IExternalPlugin : IPlugin
{
    Uri Endpoint { get; }
    int GpuMemoryMb { get; }
}
